# Unity VFX Path Library

A comprehensive path-based visual effects system for Unity that allows you to create dynamic, interactive visual effects with minimal overhead.

[![VFX Library Demo](https://img.shields.io/badge/Live%20Demo-Try%20it%20Now-blue)](https://jwho303.itch.io/vfx-library)

![VFX Library Preview](preview.gif)

![VFX](https://github.com/user-attachments/assets/b3fc0936-8585-40fc-9416-a161df830fb5)

## 🎮 Try It Live

Test the library in your browser with our [interactive WebGL demo](https://jwho303.itch.io/vfx-library).

- **Drag the black orbs** to reshape and redirect the visual effects
- **Toggle effects on/off** using the checkboxes
- Experiment with different combinations to see what's possible

## ✨ Features

- **Path-Based Visual Effects**: Create stunning effects that follow paths with intuitive controls
- **10+ Path Types**: Wave, Lightning, Vortex, Noise, Organic Wave, Bounce, Arc, ZigZag, and more
- **Multiple Render Options**: Particle systems, line renderers, and sprite sheet animations
- **Interactive & Dynamic**: Easily create responsive effects that react to gameplay events
- **Full Source Code Access**: Extend and customize to fit your specific needs
- **Editor Visualization**: Robust gizmo system for easy setup and debugging
- **Optimized Performance**: Designed with efficiency in mind for mobile and low-end devices

## 📱 Mobile Performance

This library is specifically designed to work well on mobile devices, including low-end hardware. The video below demonstrates the VFX demo scene running on a Xiaomi Redmi 9A (MediaTek Helio G25, 2GB RAM) at a consistent 30 FPS.

https://github.com/user-attachments/assets/c1faaede-d900-43be-b0e7-96f88879d67d

## 🔧 Technical Architecture

The library is built around three core components that work together:

### VFXController

The central manager class that controls and coordinates visual effects. It:
- Handles a collection of VFX elements
- Manages path points that define effect trajectories
- Controls the lifecycle (initialize, play, stop) of all attached effects
- Provides editor visualization through gizmos

### BaseVFXElement

The rendering component that visualizes effects along the path. Derived classes include:
- `LineRendererElement`: Renders effects using Unity's LineRenderer
- `ParticleElement`: Uses particle systems to visualize effects
- `SpriteSheetParticleElement`: Extends particle effects with sprite sheet animations

Each element handles its specific rendering approach while following the common path.

### BaseVFXPath

The mathematical component that calculates positions along a path. Derived classes include various path types:
- Linear paths (straight lines)
- Curved paths (arcs, waves)
- Dynamic paths (lightning, noise, organic)
- Compound paths (blended paths)

This separation of concerns allows you to mix and match different path algorithms with different rendering methods to create diverse effects.

## 🚀 Getting Started

1. Clone or download this repository
2. Import the package into your Unity project
3. Explore the example scene to understand the components
4. Create a new GameObject and add a `VFXController` component
5. Add your choice of VFX elements and path calculators
6. Configure the path points and play the effect

## 📖 Documentation

[Coming Soon - Full API documentation and tutorials]

## 🎯 Use Cases

- Magic effects in RPGs
- Sci-fi weapon trails
- Lightning and electricity effects
- Energy beams and tethers
- UI animations and transitions
- Environmental effects

## 💻 Compatibility

- **Unity Versions**: Tested on 2022.3 LTS and Unity 6.0
- **Render Pipeline**: Works with Built-in Render Pipeline (no URP/HDRP dependencies)
- **Platforms**: Tested on Windows, Android, and WebGL

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
