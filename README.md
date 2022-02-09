### Collection of small projects around simulations and visualizations

Currently uses the CPU to simulate various stuff and OpenGL to display the result in realtime to the screen.

### Usage

Load the .sln with visual studio, select and run the desired project (fastest in release mode). The projects should be named pretty self-explanatory, 
further below is a more detailed description of each of them. Most projects also contain a couple of commented out parameters to choose from that 
create interesting results and in the Windows.cs-file are general parameters like resolution or framerate that can also be changed.

### Content

1. *HeatSimulation*: Solves the heat equation for a given initial distribution
2. *SimpleWaveEquation*: Solves the linear 2D wave equation for an initial function with some additional 
  functionality for custom boundary conditions, domain properties and generator functions
3. *ReactionDiffusionSimulation*: Solves the equation for a two component reaction diffusion system for 
  some given parameters that can be edited

#### Advise for optimization or improvement is always welcome!
