#ifndef GAMEOBJECT_H
#define GAMEOBJECT_H

#include <osg/NodeCallback>
#include "newtonphysobj.h"
#include "collider.h"

class gameObject : public osg::NodeCallback
{

public:
    gameObject();

    NewtonPhysObj *physics;
    bool isScoreTrigger;

    void UpdatePhysics(float deltaTime);
    void Update_Score_Collision();
    void Set_Physics(float startMass, osg::Vec3f startPos, osg:: Vec3f startVel, osg::Vec3f startAccel);
    void Set_Sphere_Collider(float radius);
    void Set_Plane_Collider(osg::Vec3f bounds[], float coefficient_of_restitution);

    virtual void operator()(osg::Node *node, osg::NodeVisitor *nv);

};

#endif // GAMEOBJECT_H
