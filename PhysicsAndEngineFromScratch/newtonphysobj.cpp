#include "newtonphysobj.h"
#include "collider.h"
#include <osg/Vec3>

NewtonPhysObj::NewtonPhysObj()
{
    collider = new Collider();
    mass=1;
    position=osg::Vec3f(0,0,0);
    velocity=osg::Vec3f(0,0,0);
    acceleration=osg::Vec3f(0,0,0);
}
NewtonPhysObj::NewtonPhysObj(float startMass, osg::Vec3f startPos, osg:: Vec3f startVel, osg::Vec3f startAccel)
{
    collider = new Collider();
    mass=startMass;
    position=startPos;
    velocity=startVel;
    acceleration=startAccel;
}
void NewtonPhysObj::evaluate_position_time_step(float deltaTime)
{
    position += velocity*deltaTime;
    collider->position = position;
}
void NewtonPhysObj::evaluate_velocity_time_step(float deltaTime)
{
    velocity += acceleration*deltaTime;
}
void NewtonPhysObj::take_time_step(float deltaTime)
{
    evaluate_velocity_time_step(deltaTime);
    evaluate_position_time_step(deltaTime);
}
float NewtonPhysObj::get_scalar_velocity()
{
    return velocity.length();
}
void NewtonPhysObj::resolve_collision(NewtonPhysObj *other)
{
    if(this->collider->type == Collider::sphere && other->collider->type == Collider::plane)
    {
        evaluate_wall_collision(other->collider);
    }
}
void NewtonPhysObj::evaluate_wall_collision(Collider *other)
{
    osg::Vec3f newVelocity;
    newVelocity.set((velocity - other->normal*(other->normal*velocity)*2));
    velocity.set(newVelocity);
}
