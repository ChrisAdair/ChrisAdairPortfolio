#ifndef NEWTONPHYSOBJ_H
#define NEWTONPHYSOBJ_H

#include <osg/Vec3>
#include "collider.h"

class NewtonPhysObj
{
public:
    float mass;
    const float gravityConstant = 9.81f;
    float dragCoefficient;
    float area;
    float fluidMediumDensity;
    osg::Vec3f position;
    osg::Vec3f velocity;
    osg::Vec3f acceleration;
    Collider *collider;

    NewtonPhysObj();
    NewtonPhysObj(float startMass, osg::Vec3f startPos, osg:: Vec3f startVel, osg::Vec3f startAccel);

    void take_time_step(float deltaTime);
    float get_scalar_velocity();
    void resolve_collision(NewtonPhysObj *other);

private:

    void evaluate_velocity_time_step(float deltaTime);
    void evaluate_position_time_step(float deltaTime);
    void evaluate_forces_to_calculate_acceleration();
    void evaluate_wall_collision(Collider *other);
};

#endif // NEWTONPHYSOBJ_H
