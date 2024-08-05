#ifndef WORLDCONTROLLER_H
#define WORLDCONTROLLER_H
#include "osg/NodeCallback"
#include "gameobject.h"
#include <vector>
#include <osg/Node>
#include <osg/Group>
#include <osg/Vec3>

class WorldController:osg::NodeCallback
{

public:
    osg::Vec3f aimDirection;
    int score;

    WorldController(osg::ref_ptr<osg::Group> baseNode);
    void AddUpdateListener(gameObject *listener);
    void UpdatePhysics(float deltaTime);
    void CreateSphere();
    void ShootSphere();
    void CreateTarget();
    void CreateBoundingBox();
    void CreateWireframePlane(osg::Vec3f points[]);
    void CalculateCollisions();
    void ScorePoints();

    osg::ref_ptr<osg::Group> mRoot;

private:
    std::vector<gameObject*> listeners;
};

#endif // WORLDCONTROLLER_H
