#include "gameobject.h"
#include "newtonphysobj.h"
#include <osg/NodeCallback>
#include <osg/Node>
#include <osg/MatrixTransform>
#include <osg/Group>
#include <osg/Vec3f>

gameObject::gameObject()
{
    physics = new NewtonPhysObj();
    Set_Physics(1,osg::Vec3f(0,0,0),osg::Vec3(0,0,0),osg::Vec3f(0,0,0));
    isScoreTrigger = false;
}

void gameObject::UpdatePhysics(float deltaTime)
{
    deltaTime/=1000;
    physics->take_time_step(deltaTime);

}

void gameObject::operator()(osg::Node *node, osg::NodeVisitor *nv)
{
    osg::MatrixTransform *transform = dynamic_cast<osg::MatrixTransform *>(node);
    osg::Matrix m;
    m.makeTranslate(physics->position);
    transform->setMatrix(m);
    traverse(node, nv);
}

void gameObject::Set_Physics(float startMass, osg::Vec3f startPos, osg::Vec3f startVel, osg::Vec3f startAccel)
{
    physics->mass = startMass;
    physics->position = startPos;
    physics->velocity = startVel;
    physics->acceleration = startAccel;
}
void gameObject::Set_Plane_Collider(osg::Vec3f *bounds, float coefficient_of_restitution)
{
    physics->collider->type = Collider::plane;
    for(int i=0;i<3;i++)
    {
        physics->collider->planeBounds[i] = bounds[i];
    }
    physics->collider->coefficientOfRestitution = coefficient_of_restitution;
    physics->collider->EvaluateNormal();
}
void gameObject::Set_Sphere_Collider(float radius)
{
    physics->collider->type = Collider::sphere;
    physics->collider->radius = radius;
}
