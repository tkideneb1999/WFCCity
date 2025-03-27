# Wave Function Collapse City Generator
This project demonstrates an adapted implementation of the Wave Function Collapse Algorithm to generate a City (in this case Columbian themed).

**DISCLAMER** The project is lacking textures, to see the intended visual target, you can take a look at the artstation Post:
https://www.artstation.com/artwork/d0DwYx

## Algorithm Breakdown
- Initialization: Setting up the 3D grid in which the city will be generated
- Generate Rivers: Snake like algorithm to generate 4 rivers flowing through the city with one sided dilation
- Solve first Layer: Generates the Layout of the houses by collapsing tiles according to the WFC Algorithm, for the first layer only
- Generate Houses: Figures out connected elements to establish house islands then subdivides these islands into houses based on House parameters. After house division is established, a random height is assigned to a house based on the vertical Grid size. 
These houses are then solved using WFC indepedepently from one another
- Place Socket Meshes: Places meshes on predefined sockets of the meshes, also tries covering up holes between houses where one house is higher than the other

## Data Generation
Generating prototype data for the Algorithm to function properly (adjacency information) is done via a dedicated export plugin in Blender and an importer in Unity.
The Exporter generates a file containing all adjacency information by comparing the 6 different side profiles of a mesh. To speed this process up and eliminate possible misses of adjacency information, proxy meshes are used.
Additionally, types of adjacent prototypes are set to distinguish between House, River and Pavement (ground).

The Unity Project then contains an Editor tool to import this file and generate the necessary prototype assets from them as well as link them together.

For each mesh 4 prototypes are generated, covering all 4 possible 90 degree rotations.

## Pictures

![](https://cdnb.artstation.com/p/assets/images/images/053/045/283/large/benedikt-liebmann-screenshot-2022-08-23-220848.jpg?1661286249)