# Silly Race Clone Project

A 3D hyper-casual racing game prototype developed with Unity.

This project is a clone-style implementation inspired by **Silly Race**, focusing on simple race mechanics, obstacle interaction, AI opponents, level progression, and mobile-friendly gameplay flow.

## About the Project

The main goal of this project was to practice core Unity game development concepts by recreating the basic gameplay loop of a casual runner/racing game.

The player competes against AI-controlled opponents on an obstacle-based track. Opponents are controlled with Unity NavMesh and are able to chase, move, and avoid obstacles during the race.

The project is still under development and some gameplay mechanics may need further polishing.

## Features

- 3D race gameplay
- Player movement system
- AI opponent movement
- NavMesh-based opponent navigation
- Obstacle avoidance mechanics
- Race ranking system
- Finish/end zone logic
- Restart functionality
- Basic UI flow
- Camera follow system
- Screenshot showcase

## Technologies Used

- Unity 3D
- C#
- Unity NavMesh
- DOTween
- Cinemachine
- TextMesh Pro
- Third-party skybox assets

## Project Structure

```text
Assets/
├── Animations/
├── Plugins/
├── Prefabs/
├── Resources/
├── Scenes/
├── Scripts/
│   ├── ObstacleScripts/
│   ├── CanvasController.cs
│   ├── EndZone.cs
│   ├── GameManager.cs
│   ├── OpponentManager.cs
│   ├── PaintingManager.cs
│   ├── PlayerManager.cs
│   ├── RankManager.cs
│   ├── RestartButton.cs
│   ├── camController.cs
│   └── touchInput.cs
├── Sprites/
├── TextMesh Pro/
└── _3rd_Party/
```

# Screenshots
![](https://github.com/ksensazli/Silly-Race-Clone-Project/blob/master/Screenshots/SS-1.jpg) <br />
![](https://github.com/ksensazli/Silly-Race-Clone-Project/blob/master/Screenshots/SS-2.jpg) <br />
![](https://github.com/ksensazli/Silly-Race-Clone-Project/blob/master/Screenshots/SS-3.jpg) <br />
![](https://github.com/ksensazli/Silly-Race-Clone-Project/blob/master/Screenshots/SS-4.jpg)

## Development Notes

This project was created as a learning-oriented Unity prototype.
The current version focuses on implementing the core mechanics rather than delivering a fully polished commercial game.

Some areas that can be improved:

- More polished character animations
- Improved obstacle behavior
- Better AI balancing
- Additional levels
- Mobile build optimization
- Sound effects and music
- More detailed UI/UX
