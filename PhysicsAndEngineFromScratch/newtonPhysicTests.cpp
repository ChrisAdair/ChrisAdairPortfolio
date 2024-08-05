#include <gtest/gtest.h>
#include "newtonphysobj.h"
#include <osg/Vec3>

TEST(newtonPhysicsTests, construct_newtonian_object_and_set_initial_conditions)
{
    NewtonPhysObj obj;
    obj.mass = 1;
    osg::Vec3f pos;
    osg::Vec3f vel;
    osg::Vec3f accel;
    EXPECT_NO_THROW(obj.position = pos);
    EXPECT_NO_THROW(obj.velocity = vel);
    EXPECT_NO_THROW(obj.acceleration = accel);


}
TEST(newtonPhysicsTests, test_acceleration_effect_on_velocity_steps)
{
    osg::Vec3f initialPosition(1,0,0);
    osg::Vec3f initialVelocity(0,0,0);
    osg::Vec3f initialAcceleration(1,0,0);
    NewtonPhysObj testObj(1,initialPosition,initialVelocity,initialAcceleration);
    for(int step=0;step<10;step++)
    {
        testObj.take_time_step(1);
    }
    osg::Vec3f testCase(10,0,0);
    EXPECT_EQ(testCase,testObj.velocity);
}
TEST(newtonPhysicsTests, test_acceleration_effect_on_position_steps)
{
    osg::Vec3f initialPosition(1,0,0);
    osg::Vec3f initialVelocity(0,0,0);
    osg::Vec3f initialAcceleration(1,0,0);
    NewtonPhysObj testObj(1,initialPosition,initialVelocity,initialAcceleration);
    for(int step=0;step<10;step++)
    {
        testObj.take_time_step(1);
    }
    osg::Vec3f testCase(56,0,0);
    EXPECT_EQ(testCase,testObj.position);
}
TEST(newtonPhysicsTests, test_ball_vs_wall_collision_gives_correct_reflection_vector)
{
    NewtonPhysObj *wall = new NewtonPhysObj();
    osg::Vec3f rightHandRuleBounds[]{osg::Vec3f(0,0,0), osg::Vec3f(0,1,0), osg::Vec3f(1,0,0)};
    for(int i=0;i<3;i++)
    {
        wall->collider->planeBounds[i] = rightHandRuleBounds[i];
    }
    wall->collider->type = Collider::plane;
    wall->collider->EvaluateNormal();
    NewtonPhysObj *sphere = new NewtonPhysObj();
    sphere->collider->type = Collider::sphere;
    sphere->collider->radius = 1;
    sphere->position.set(0,0,.5f);
    sphere->velocity.set(0,0,-1);
    sphere->resolve_collision(wall);

    EXPECT_EQ(1,sphere->velocity.z());
    delete wall;
    delete sphere;
}
