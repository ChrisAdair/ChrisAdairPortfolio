import scipy.io as matIo
from scipy.spatial.transform import Rotation as R
import numpy as np
d = matIo.loadmat("n4-id1Encrypt 1 1-10-2022-04-10.mat",squeeze_me=True)
quats = d['tHist'][:,1] # These are the quaternions of all grains at each time step
Deff = d['tHist'][:,2] # This is the effective diffusivity at each time step
Action = d['tHist'][:,0] # This is the action taken at each time step
ActGrain = d['tHist'][:,3] # This is the grain the action was taken on
dual = d['structureUsed']['Dual'] # This is the dual of the GBN / the position encoding of the action space
nGrains = quats[0].shape[0]
test = d['tHist'][:,1][1]
print(f'Shape of first quaternion is {test.shape}')

for i in range(4):
    print(f'Quaternion ',i,' ',d['tHist'][:,1][1][i,:])
    #print(f'DeltaQuaternion ',i,' ',d['tHist'][:,1][2][i,:]-d['tHist'][:,1][1][i,:])

testQ = R.from_quat(test[:,[1,2,3,0]])
print(testQ.as_quat())

actDict = {'none':0,'ori':1,'OptGrain':2,'Undo':3,'anneal':4}
output = np.zeros([Action.size,nGrains])
ident = np.array([0., 1., 0., 0.])
identSet = np.tile(ident,(nGrains,1))

for i in range(Action.size):
    output[i,ActGrain[i]-1] = actDict[Action[i]]
    
fullSet = np.empty(Action.size+1, dtype=np.ndarray)
fullSet[0] = identSet
for i in range(0,len(quats)):
    fullSet[i+1] = quats[i]

print(fullSet, "\n")
print(output, " \n")
print(len(fullSet), " ", len(output)," ",len(quats))