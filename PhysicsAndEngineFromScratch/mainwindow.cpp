#include "mainwindow.h"
#include "ui_mainwindowform.h"

#include <QDockWidget>
#include <QObject>
#include <QPushButton>
#include <QSlider>

MainWindow::MainWindow(QWidget *parent) :
    QMainWindow{parent},
    mMainWindowUI{new Ui::MainWindowForm}
{
    mMainWindowUI->setupUi(this);
    QPushButton *fireButton = mMainWindowUI->centralWidget->findChild<QPushButton*>("FireButton");
    QSlider *angleSlider = mMainWindowUI->centralWidget->findChild<QSlider*>("horizontalSlider");
    QSlider *pitchSlider = mMainWindowUI->centralWidget->findChild<QSlider*>("verticalSlider");
    connect(fireButton, SIGNAL(clicked()), mMainWindowUI->openGLWidget, SLOT(ButtonPress()));
    connect(angleSlider, SIGNAL(valueChanged(int)), mMainWindowUI->openGLWidget, SLOT(UpdateAngle(int)));
    connect(pitchSlider, SIGNAL(valueChanged(int)), mMainWindowUI->openGLWidget, SLOT(UpdatePitch(int)));

}

MainWindow::~MainWindow()
{
    delete mMainWindowUI;
}

void MainWindow::on_actionExit_triggered()
{
    QApplication::quit();
}
