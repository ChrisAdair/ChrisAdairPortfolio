import scipy.io as matIo
from scipy.spatial.transform import Rotation as R
import torch
import scipy as sp
import numpy as np
from pathlib import Path
import time

class GameSimulation:

    def __init__(self, file) -> None:
        self.gameData = matIo.loadmat(f'{Path(__file__).parent}/EvalData/'+ file,squeeze_me=True)
        self.MaxScore = self.gameData['maxScore']
        self.dual = self.gameData['structureUsed']['Dual'].tolist() # This is the dual of the GBN / the position encoding of the action space
        self.nGrains = self.gameData['structureUsed']['Dual'].tolist().shape[0]
        self.quats = np.stack(self.gameData['tHist'][:,1],dtype=np.float64)
        normals = self.gameData['structureUsed']['Normal'].tolist()
        self.edges = self.gameData['structureUsed']['Edges'].tolist()
        self.areaGB = self.gameData['structureUsed']['AreaGB'].tolist()
        self.edgeLength = self.gameData['structureUsed']['EdgeLength'].tolist()
        self.nVertices = self.gameData['structureUsed']['vSink'].tolist()
        self.edgeGrains = self.gameData['structureUsed']['EdgeGrains'].tolist()
        self.grainPairs = self.gameData['structureUsed']['grainPairs'].tolist()
        self.grainPairEdges = self.gameData['structureUsed']['grainPairEdges'].tolist()
        lSample = self.gameData['structureUsed']['LengthSample']
        wSample = self.gameData['structureUsed']['WidthSample']
        tSample = self.gameData['structureUsed']['ThicknessSample']
        self.sampleScale = lSample/(wSample*tSample)
        normIdx = [np.where(self.grainPairEdges == i+1)[0][0] for i in range(self.grainPairs.shape[0])]
        nX = np.sin(normals[normIdx,1])*np.cos(normals[normIdx,0])
        nY = np.sin(normals[normIdx,1])*np.sin(normals[normIdx,0])
        nZ = np.cos(normals[normIdx,1])
        self.grainOnlyNorms = np.abs(nX) + np.abs(nY) + np.abs(nZ)
        nX = np.sin(normals[:,1])*np.cos(normals[:,0])
        nY = np.sin(normals[:,1])*np.sin(normals[:,0])
        nZ = np.cos(normals[:,1])
        self.normDeff = np.abs(nX) + np.abs(nY) + np.abs(nZ)
        self.perms = self.faster_permutations(4)
        initState = np.zeros((self.nGrains, 4))
        initState[:,1] = 1
        self.prevQ = []
        self.prevQ.append(initState)
        #Misorientation equations
        self.eq = []
        self.eq.append([lambda a0,a1,a2,a3: a0, lambda a0,a1,a2,a3: a1, lambda a0,a1,a2,a3: a2, lambda a0,a1,a2,a3: a3])
        self.eq.append([lambda a0,a1,a2,a3: (2.0**-0.5)*(a0+a1), lambda a0,a1,a2,a3: (2.0**-0.5)*(a0-a1), lambda a0,a1,a2,a3: (2.0**-0.5)*(a2+a3), lambda a0,a1,a2,a3: (2.0**-0.5)*(a2-a3)])
        self.eq.append([lambda a0,a1,a2,a3: (2.0**-0.5)*(a0+a2), lambda a0,a1,a2,a3: (2.0**-0.5)*(a0-a2), lambda a0,a1,a2,a3: (2.0**-0.5)*(a1+a3), lambda a0,a1,a2,a3: (2.0**-0.5)*(a1-a3)])
        self.eq.append([lambda a0,a1,a2,a3: (2.0**-0.5)*(a0+a3), lambda a0,a1,a2,a3: (2.0**-0.5)*(a0-a3), lambda a0,a1,a2,a3: (2.0**-0.5)*(a1+a2), lambda a0,a1,a2,a3: (2.0**-0.5)*(a1-a2)])
        self.eq.append([lambda a0,a1,a2,a3: 0.5*(a0+a1+a2+a3), lambda a0,a1,a2,a3: 0.5*(a0+a1-a2-a3), lambda a0,a1,a2,a3: 0.5*(a0-a1+a2-a3), lambda a0,a1,a2,a3: 0.5*(a0-a1-a2+a3)])
        self.eq.append([lambda a0,a1,a2,a3: 0.5*(a0+a1+a2-a3), lambda a0,a1,a2,a3: 0.5*(a0+a1-a2+a3), lambda a0,a1,a2,a3: 0.5*(a0-a1+a2+a3), lambda a0,a1,a2,a3: 0.5*(a0-a1-a2-a3)])

        #Sign changes
        self.s = np.zeros((16,4))
        ind = 0
        for i in [-1,1]:
            for j in [-1,1]:
                for k in [-1,1]:
                    for l in [-1,1]:
                        self.s[ind,:] = [i, j, k, l]
                        ind += 1
        self.gARotations = []
        for i in range(3):
            for j in [1, -1]:
                rot = np.zeros(3)
                rot[i] = j
                self.gARotations.append(R.from_rotvec(rot, degrees=True))

    def evalGBN(self, state):

        D = self.disorientation(self.misorientation(state[self.grainPairs[:,0]-1,:], state[self.grainPairs[:,1]-1,:]))
        dLink = self.diffusionModel(D[self.grainPairEdges-1])
        DiffusanceGBSegment = dLink*self.areaGB/self.edgeLength #-1 for matlab indexing
        Wv = sp.sparse.csr_matrix((np.concatenate((DiffusanceGBSegment,DiffusanceGBSegment)), (np.concatenate((self.edges[:,0],self.edges[:,1]))-1, np.concatenate((self.edges[:,1],self.edges[:,0]))-1)))
        Wv.eliminate_zeros()
        #Wv[self.edges[:,0]-1, self.edges[:,1]-1] = DiffusanceGBSegment
        #Wv[self.edges[:,1]-1, self.edges[:,0]-1] = DiffusanceGBSegment
        #Wv += np.transpose(Wv)
        Sv = np.diag(np.squeeze(np.sum(Wv, axis=0).A))
        L = Sv - Wv.A
        [lam, Uv] = sp.linalg.eigh(L)
        RSample = np.sum((1/lam[1:])*(Uv[-2,1:]-Uv[-1,1:])**2)
        Deff = (1/RSample)*self.sampleScale
        return Deff

    def diffusionModel(self, q):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (angle/10 + self.normDeff) + 1)
        
        return D
    
    def misorientation(self, q1, q2):
        qr1 = R.from_quat(np.roll(q1, -1, axis=1))
        qr2 = R.from_quat(np.roll(q2, -1, axis=1))
        miss = np.roll((qr1.inv()*qr2).as_quat(canonical=True), 1, axis=1)
        return miss
    
    def disorientation(self, q):
        
        nm = q.shape[0]

        D = np.zeros((nm,4)) #Preallocate
        for i in range(6):
            for j in range(16):
                for k in range(self.perms.shape[0]):

                    a = np.array(self.eq[i][self.perms[k,0]](q[:,0], q[:,1], q[:,2], q[:,3])*self.s[j,0])
                    b = np.array(self.eq[i][self.perms[k,1]](q[:,0], q[:,1], q[:,2], q[:,3])*self.s[j,1])
                    c = np.array(self.eq[i][self.perms[k,2]](q[:,0], q[:,1], q[:,2], q[:,3])*self.s[j,2])
                    d = np.array(self.eq[i][self.perms[k,3]](q[:,0], q[:,1], q[:,2], q[:,3])*self.s[j,3])

                    isdis = ((b >= c) & (c >= d) & (d >= 0)) & ((b <= (np.sqrt(2)-1)*a)) & (b+c+d <= a)
                    sup1 = isdis & (a == (np.sqrt(2)+1)*b)

                    if any(sup1):
                        isdis[sup1] = c[sup1] <= (np.sqrt(2)+1)*d[sup1]
                    if any(isdis):
                        D[isdis,:] = np.stack([a[isdis], b[isdis], c[isdis], d[isdis]], axis=1)

        return D
    
    # From https://stackoverflow.com/questions/64291076/generating-all-permutations-efficiently
    def faster_permutations(self, n):
        # empty() is fast because it does not initialize the values of the array
        # order='F' uses Fortran ordering, which makes accessing elements in the same column fast
        perms = np.empty((np.math.factorial(n), n), dtype=np.uint8, order='F')
        perms[0, 0] = 0

        rows_to_copy = 1
        for i in range(1, n):
            perms[:rows_to_copy, i] = i
            for j in range(1, i + 1):
                start_row = rows_to_copy * j
                end_row = rows_to_copy * (j + 1)
                splitter = i - j
                perms[start_row: end_row, splitter] = i
                perms[start_row: end_row, :splitter] = perms[:rows_to_copy, :splitter]  # left side
                perms[start_row: end_row, splitter + 1:i + 1] = perms[:rows_to_copy, splitter:i]  # right side

            rows_to_copy *= i + 1

        return perms
    
    def step(self, action):
        
        q = np.copy(self.prevQ[-1])
        # Do the action on the structure
        a = np.nonzero(action)[0][0]
        match action[a]:
            case 1:
                newq = self.manualStep(a, q)
                self.prevQ.append(newq)
            case 2:
                newq = self.gradAscent(a, q)
                self.prevQ.append(newq)
            case 3:
                newq = self.undo(a)
            case 4:
                newq = self.randomStep(a, q)
                self.prevQ.append(newq)
            case _:
                pass
        # Package output
        if len(self.prevQ) > 20:
            self.prevQ.pop(0)
        state = self.prevQ[-1]
        Deff = self.evalGBN(state)
        
        if Deff > self.MaxScore*0.9:
            done = True
        else:
            done = False
        posEncoding = self.getPosEncoding(state)
        # Pad to the state dimensions

        return done, Deff, state, posEncoding
    
    def manualStep(self, a, q):
        # I do not have a way to get the manual movememnt of a player
        # Instead all steps are gradient ascent
        q = self.gradAscent(a, q)

        return q
    
    def randomStep(self, a, q):

        q[a,:] = np.roll(R.random().as_quat(canonical=True), 1, axis=0)
        return q
    
    def gradAscent(self, a, q):
        
        tolerance = 0.0001
        qAll = R.from_quat(np.roll(q, -1, axis=1))
        q1 = qAll[a]
        idx = (self.grainPairs == a+1).nonzero() # Plus 1 for matlab indexing
        qDual = qAll[self.grainPairs[idx[0],idx[1]-1]-1] 
        start = time.time()
        deltaD = 1
        scale = 26
        firstD = self.getNetDiffusionNeighbors(q1, qDual, idx[0])
        tempD = firstD
        while deltaD > tolerance:
            highestGrad = R.identity()
            itTime = time.time()
            for i, rot in enumerate(self.gARotations):
                q1temp = (rot**scale)*q1
                D = self.getNetDiffusionNeighbors(q1temp, qDual, idx[0])
                if D > tempD:
                    deltaD = D - firstD
                    tempD = D
                    highestGrad = rot
            q1 = (highestGrad**scale)*q1
            if (highestGrad.approx_equal(R.identity())) & (scale >1):
                scale-=5
            else:
                deltaD = tempD-firstD
                firstD = tempD
            print(f'totTime: {time.time()-start} \n iterTime: {time.time()-itTime}')
        q[a,:] = np.roll(q1.as_quat(), 1, axis=1)
        return q
    
    def getNetDiffusionNeighbors(self, q1, qDual, subset):
        disSet = np.squeeze(np.roll((q1.inv()*qDual).as_quat(), 1, axis=1))
        assert disSet.shape[1] == 4
        temp = self.disorientation(disSet)
        D = np.sum(self.diffusionModelPartial(temp, subset))
        return D
    
    def diffusionModelPartial(self, q, subset):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (angle/10 + self.grainOnlyNorms[subset]) + 1)
        
        return D
    
    def undo(self, a):
        q = self.prevQ[-2]
        self.prevQ.pop(-1)
        return q
    
    def diffusionModelGrain(self, q):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (angle/10 + self.grainOnlyNorms) + 1)
        
        return D
    def diffusionModelParabolic(self, q):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (-0.006369*angle**2 + 0.4*angle + self.normDeff) + 1)
        
        return D
    
    def diffusionModelParabolicGrain(self, q):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (-0.006369*angle**2 + 0.4*angle + self.grainOnlyNorms) + 1)
        return D
    
    def diffusionModelParabolicSub(self, q, subset):
        angle = 2 * np.rad2deg(np.arccos(q[:,0]))
        D = np.abs(1e7 * (-0.006369*angle**2 + 0.4*angle + self.grainOnlyNorms[subset]) + 1)
        return D
    
    def getPosEncoding(self, state):
        D = self.disorientation(self.misorientation(state[self.grainPairs[:,0]-1,:], state[self.grainPairs[:,1]-1,:]))
        dLink = self.diffusionModelGrain(D)
        dual = np.zeros((self.nGrains,self.nGrains))
        dual[self.grainPairs[:,0]-1, self.grainPairs[:,1]-1] = dLink
        dual += dual.transpose()
        trash, dualEigs = np.linalg.eigh(dual)
        return dualEigs[:,:4]

if __name__ == '__main__':
    #Test suite for implementation
    sim = GameSimulation('n10-id8Encrypt 6 12-11-2022-02-05.mat')

    sim.evalGBN(sim.prevQ[-1])
    sim.evalGBN(sim.quats[0,:])
    sim.step(np.array([1, 0, 0, 0, 0, 0, 0, 0, 0, 0]))
    sim.step(np.array([1, 0, 0, 0, 0, 0, 0, 0, 0, 0]))
    #sim.step(np.array([0, 0, 4, 0, 0, 0, 0, 0, 0, 0]))
    #sim.step(np.array([0, 0, 3, 0, 0, 0, 0, 0, 0, 0]))
    #Test up to here