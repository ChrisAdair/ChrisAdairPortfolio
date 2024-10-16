# Christopher Adair

## Operation: Forge the Deep
I was the team lead and head developer of the video game [Operation: Forge the Deep](https://store.steampowered.com/app/2053100/Operation_Forge_the_Deep/). This game was made in collaboration with BYU Animation, with the purpose of providing solutions to real materials science problems. This resulted in two publications, [Microstructure design using a human computation game](https://www.sciencedirect.com/science/article/abs/pii/S2589152922002265), and "A Decision Transformer Approach to GBN Optimization", which is currently under peer-review.

![Screenshot of Operation: Forge the Deep](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2053100/ss_691e2ef49fdc7ed965ec7fce5c091c8d65a876b8.1920x1080.jpg)

This game was built using Unity. Highlights of my contributions include:
- UDP networking for multiplayer functionality
  - Unity deprecated multiplayer code during the development, therefore a replacement was implemented
  - Lobby and matchmaking implementations
  - Private party and invite implementations
  - Data collection and action recording to server storage
- Timeline extension, enabling animation students to create custom cutscenes without code
- Database setup, security, and access for player information storage through MySQL
- All gameplay mechanics, calculations, and parallelization
  - Implementation of performant Intel MKL libraries and wrapping for C# use
  - Computational modeling of materials science problem: Grain boundary network property optimizaztion
  - Optimization of computational model to achieve frame rate goals

## Machine learning through Decision Transformers

ML model modified from Decision Transformer and Multi-Game Decision Transformer to be trained on data gathered from [Operation: Forge the Deep](https://store.steampowered.com/app/2053100/Operation_Forge_the_Deep/). This model was made using PyTorch, and analyzed using Matlab.

Details on model performance can be found in "A Decision Transformer Approach to GBN Optimization", which is currently under peer-review.

The model is capable of solving the same puzzles as in Operation: Forge the Deep, with similar performance to the median player.

## Test driven development of Physics Engine and Game Engine

"Physics and Engine from Scratch" includes examples of my TDD coding work, as well as simple implementations of physics collisions from scratch and simulation coding in Open Scene Graph.

## Product Development

In addition to software, I have also designed multiple mechanical systems and products. One example is a student project for a marble track system meant for aesthetic task timing.
![Rendering of Marble Pomodoro Timer](/HQRender576Proj2.png)

My contributions to this product were in opportunity definition, concept selection, CAD drawings, GD&T for marble dispensing, STL files for manufacturing, and prototype assembly. This project was also completed during the onset of COVID-19 lockdowns, which necessitated the use of remote collaboration tools such as [OnShape](https://www.onshape.com/en/) to achieve progress.
