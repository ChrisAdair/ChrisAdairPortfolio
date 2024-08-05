cmake_minimum_required(VERSION 3.4)
project(QtOSGExample)

FIND_PACKAGE(Qt5Widgets)
FIND_PACKAGE(Qt5Gui)
FIND_PACKAGE( OpenSceneGraph REQUIRED COMPONENTS osgDB osgGA osgUtil osgViewer osgText)

INCLUDE_DIRECTORIES( ${OPENSCENEGRAPH_INCLUDE_DIRS} )

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED 1)
SET(CMAKE_INCLUDE_CURRENT_DIR ON)
SET(CMAKE_AUTOMOC ON)
SET(CMAKE_AUTOUIC ON)
SET(CMAKE_AUTORCC ON)

IF (NOT TARGET gtest)
    # Prevent GoogleTest from overriding our compiler/linker options
    # when building with Visual Studio
    set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)

    add_subdirectory(googletest)
ENDIF()

ADD_LIBRARY(newtonianPhysics STATIC newtonphysobj.cpp newtonphysobj.h collider.cpp collider.h)

SET(MYSOURCE
    main.cpp
    mainwindow.cpp
    mainwindow.h
    mainwindowform.ui
    osgwidget.h
    osgwidget.cpp
    gameObject.cpp gameObject.h worldcontroller.cpp worldcontroller.h
    )

add_executable(${PROJECT_NAME}
    ${MYSOURCE}
    )

target_link_libraries(${PROJECT_NAME}
    ${OPENSCENEGRAPH_LIBRARIES}	
    Qt5::Widgets
    Qt5::Gui
    newtonianPhysics
)

add_executable(unitTests
    newtonPhysicTests.cpp
    colliderTests.cpp)
target_link_libraries(unitTests
    gtest gtest_main
    newtonianPhysics)
