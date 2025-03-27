# Wave Function Collapse City Generator
This project demonstrates an adapted implementation of the Wave Function Collapse Algorithm to generate a City (in this case Columbian themed).

**DISCLAMER** The project is lacking textures, to see the intended visual target, you can take a look at the artstation Post:
https://www.artstation.com/artwork/d0DwYx

## Algorithm Breakdown
- Initialization: Setting up the 3D grid in which the city will be generated
- Generate Rivers: Snake like algorithm to generate 4 rivers flowing through the city with one sided dilation
- Solve first Layer: Generates the Layout of the houses by collapsing tiles according to the WFC Algorithm, for the first layer only
- Generate Houses: Figures out Houses via flood fill and adjacency information that has been provided by the Prototypes. These islands are then subdivides into smaller houses based on the house size parameters.
Afterwards, random heights between 2 and the height of the 3D grid are assigned to each house. Then each individual house is being solved via Wave Function Collapse.
- Place Socket Meshes: Places meshes on predefined sockets of the meshes. Some of these sockets are specifically placed to give the option to cover up holes, which might occur due to different house heights.

## Data Generation
Generating prototype data for the Algorithm to function properly (adjacency information) is done via a dedicated export plugin in Blender and an importer in Unity.
The Exporter generates a file containing all adjacency information by comparing the 6 different side profiles of a mesh. To speed this process up and eliminate possible misses of adjacency information, proxy meshes are used.
Additionally, types of adjacent prototypes are set to distinguish between House, River and Pavement (ground).

The Unity Project then contains an Editor tool to import this file and generate the necessary prototype assets from them as well as link them together.

For each mesh 4 prototypes are generated, covering all 4 possible 90 degree rotations. These are then assigned to the WFC Script

## Pictures

![](https://cdnb.artstation.com/p/assets/images/images/053/045/283/large/benedikt-liebmann-screenshot-2022-08-23-220848.jpg?1661286249)
![](https://cdnb.artstation.com/p/assets/images/images/053/045/263/large/benedikt-liebmann-screenshot-2022-08-23-220748.jpg?1661286218)
![](https://cdnb.artstation.com/p/assets/images/images/053/045/239/large/benedikt-liebmann-screenshot-2022-08-23-220548.jpg?1661287383)
