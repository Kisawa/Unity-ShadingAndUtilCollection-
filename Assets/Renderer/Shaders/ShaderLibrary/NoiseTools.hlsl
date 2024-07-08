#ifndef NOISE_TOOLS_INCLUDED
#define NOISE_TOOLS_INCLUDED

#include "RandomTools.hlsl"

//Value Noise
float ValueNoise(float value)
{
    float previousCellNoise = rand1dTo1d(floor(value));
    float nextCellNoise = rand1dTo1d(ceil(value));
    float interpolator = frac(value);
    interpolator = easeInOut(interpolator);
    float noise = lerp(previousCellNoise, nextCellNoise, interpolator);
    return noise;
}

float ValueNoise(float2 value)
{
    float upperLeftCell = rand2dTo1d(float2(floor(value.x), ceil(value.y)));
    float upperRightCell = rand2dTo1d(float2(ceil(value.x), ceil(value.y)));
    float lowerLeftCell = rand2dTo1d(float2(floor(value.x), floor(value.y)));
    float lowerRightCell = rand2dTo1d(float2(ceil(value.x), floor(value.y)));

    float interpolatorX = easeInOut(frac(value.x));
    float interpolatorY = easeInOut(frac(value.y));

    float upperCells = lerp(upperLeftCell, upperRightCell, interpolatorX);
    float lowerCells = lerp(lowerLeftCell, lowerRightCell, interpolatorX);

    float noise = lerp(lowerCells, upperCells, interpolatorY);
    return noise;
}

float2 ValueNoise2d(float2 value)
{
    float2 upperLeftCell = rand2dTo2d(float2(floor(value.x), ceil(value.y)));
    float2 upperRightCell = rand2dTo2d(float2(ceil(value.x), ceil(value.y)));
    float2 lowerLeftCell = rand2dTo2d(float2(floor(value.x), floor(value.y)));
    float2 lowerRightCell = rand2dTo2d(float2(ceil(value.x), floor(value.y)));

    float interpolatorX = easeInOut(frac(value.x));
    float interpolatorY = easeInOut(frac(value.y));

    float2 upperCells = lerp(upperLeftCell, upperRightCell, interpolatorX);
    float2 lowerCells = lerp(lowerLeftCell, lowerRightCell, interpolatorX);

    float2 noise = lerp(lowerCells, upperCells, interpolatorY);
    return noise;
}

float ValueNoise(float3 value) 
{
    float interpolatorX = easeInOut(frac(value.x));
    float interpolatorY = easeInOut(frac(value.y));
    float interpolatorZ = easeInOut(frac(value.z));

    float cellNoiseZ[2];
    [unroll]
    for (int z = 0; z <= 1; z++) 
    {
        float cellNoiseY[2];
        [unroll]
        for (int y = 0; y <= 1; y++) 
        {
            float cellNoiseX[2];
            [unroll]
            for (int x = 0; x <= 1; x++) 
            {
                float3 cell = floor(value) + float3(x, y, z);
                cellNoiseX[x] = rand3dTo1d(cell);
            }
            cellNoiseY[y] = lerp(cellNoiseX[0], cellNoiseX[1], interpolatorX);
        }
        cellNoiseZ[z] = lerp(cellNoiseY[0], cellNoiseY[1], interpolatorY);
    }
    float noise = lerp(cellNoiseZ[0], cellNoiseZ[1], interpolatorZ);
    return noise;
}

float3 ValueNoise3d(float3 value)
{
    float interpolatorX = easeInOut(frac(value.x));
    float interpolatorY = easeInOut(frac(value.y));
    float interpolatorZ = easeInOut(frac(value.z));

    float3 cellNoiseZ[2];
    [unroll]
    for (int z = 0; z <= 1; z++) 
    {
        float3 cellNoiseY[2];
        [unroll]
        for (int y = 0; y <= 1; y++) 
        {
            float3 cellNoiseX[2];
            [unroll]
            for (int x = 0; x <= 1; x++) 
            {
                float3 cell = floor(value) + float3(x, y, z);
                cellNoiseX[x] = rand3dTo3d(cell);
            }
            cellNoiseY[y] = lerp(cellNoiseX[0], cellNoiseX[1], interpolatorX);
        }
        cellNoiseZ[z] = lerp(cellNoiseY[0], cellNoiseY[1], interpolatorY);
    }
    float3 noise = lerp(cellNoiseZ[0], cellNoiseZ[1], interpolatorZ);
    return noise;
}


//Perlin Noise
float PerlinNoise(float value)
{
    float fraction = frac(value);
    float interpolator = easeInOut(fraction);

    float previousCellInclination = rand1dTo1d(floor(value)) * 2 - 1;
    float previousCellLinePoint = previousCellInclination * fraction;

    float nextCellInclination = rand1dTo1d(ceil(value)) * 2 - 1;
    float nextCellLinePoint = nextCellInclination * (fraction - 1);
    return lerp(previousCellLinePoint, nextCellLinePoint, interpolator);
}

float PerlinNoise(float2 value) 
{
    float2 lowerLeftDirection = rand2dTo2d(float2(floor(value.x), floor(value.y))) * 2 - 1;
    float2 lowerRightDirection = rand2dTo2d(float2(ceil(value.x), floor(value.y))) * 2 - 1;
    float2 upperLeftDirection = rand2dTo2d(float2(floor(value.x), ceil(value.y))) * 2 - 1;
    float2 upperRightDirection = rand2dTo2d(float2(ceil(value.x), ceil(value.y))) * 2 - 1;

    float2 fraction = frac(value);

    float lowerLeftFunctionValue = dot(lowerLeftDirection, fraction - float2(0, 0));
    float lowerRightFunctionValue = dot(lowerRightDirection, fraction - float2(1, 0));
    float upperLeftFunctionValue = dot(upperLeftDirection, fraction - float2(0, 1));
    float upperRightFunctionValue = dot(upperRightDirection, fraction - float2(1, 1));

    float interpolatorX = easeInOut(fraction.x);
    float interpolatorY = easeInOut(fraction.y);

    float lowerCells = lerp(lowerLeftFunctionValue, lowerRightFunctionValue, interpolatorX);
    float upperCells = lerp(upperLeftFunctionValue, upperRightFunctionValue, interpolatorX);

    float noise = lerp(lowerCells, upperCells, interpolatorY);
    return noise;
}

float PerlinNoise(float3 value) 
{
    float3 fraction = frac(value);

    float interpolatorX = easeInOut(fraction.x);
    float interpolatorY = easeInOut(fraction.y);
    float interpolatorZ = easeInOut(fraction.z);

    float cellNoiseZ[2];
    [unroll]
    for (int z = 0; z <= 1; z++) 
    {
        float cellNoiseY[2];
        [unroll]
        for (int y = 0; y <= 1; y++) 
        {
            float cellNoiseX[2];
            [unroll]
            for (int x = 0; x <= 1; x++) 
            {
                float3 cell = floor(value) + float3(x, y, z);
                float3 cellDirection = rand3dTo3d(cell) * 2 - 1;
                float3 compareVector = fraction - float3(x, y, z);
                cellNoiseX[x] = dot(cellDirection, compareVector);
            }
            cellNoiseY[y] = lerp(cellNoiseX[0], cellNoiseX[1], interpolatorX);
        }
        cellNoiseZ[z] = lerp(cellNoiseY[0], cellNoiseY[1], interpolatorY);
    }
    float noise = lerp(cellNoiseZ[0], cellNoiseZ[1], interpolatorZ);
    return noise;
}

float3 PerlinNoise3d(float3 value)
{
    float3 fraction = frac(value);

    float interpolatorX = easeInOut(fraction.x);
    float interpolatorY = easeInOut(fraction.y);
    float interpolatorZ = easeInOut(fraction.z);

    float3 cellNoiseZ[2];
    [unroll]
    for (int z = 0; z <= 1; z++)
    {
        float3 cellNoiseY[2];
        [unroll]
        for (int y = 0; y <= 1; y++)
        {
            float3 cellNoiseX[2];
            [unroll]
            for (int x = 0; x <= 1; x++)
            {
                float3 cell = floor(value) + float3(x, y, z);
                float3 cellDirection = rand3dTo3d(cell) * 2 - 1;
                float3 compareVector = fraction - float3(x, y, z);
                cellNoiseX[x] = cross(cellDirection, compareVector);
            }
            cellNoiseY[y] = lerp(cellNoiseX[0], cellNoiseX[1], interpolatorX);
        }
        cellNoiseZ[z] = lerp(cellNoiseY[0], cellNoiseY[1], interpolatorY);
    }
    float3 noise = lerp(cellNoiseZ[0], cellNoiseZ[1], interpolatorZ);
    return noise;
}


//Layered Noise
#define OCTAVES 4
float LayeredNoise(float value, float roughness, float persistance)
{
    float noise = 0;
    float frequency = 1;
    float factor = 1;
    [unroll]
    for (int i = 0; i < OCTAVES; i++) 
    {
        noise = noise + PerlinNoise(value * frequency + i * 0.72354) * factor;
        factor *= persistance;
        frequency *= roughness;
    }
    return noise;
}

float LayeredNoise(float2 value, float roughness, float persistance)
{
    float noise = 0;
    float frequency = 1;
    float factor = 1;
    [unroll]
    for (int i = 0; i < OCTAVES; i++) 
    {
        noise = noise + PerlinNoise(value * frequency + i * 0.72354) * factor;
        factor *= persistance;
        frequency *= roughness;
    }
    return noise;
}

float LayeredNoise(float3 value, float roughness, float persistance)
{
    float noise = 0;
    float frequency = 1;
    float factor = 1;
    [unroll]
    for (int i = 0; i < OCTAVES; i++)
    {
        noise = noise + PerlinNoise(value * frequency + i * 0.72354) * factor;
        factor *= persistance;
        frequency *= roughness;
    }
    return noise;
}


//Voronoi Noise
float2 VoronoiNoise(float2 value)
{
    float2 baseCell = floor(value);
    float minDistToCell = 10;
    float2 closestCell;
    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            float2 cell = baseCell + float2(x, y);
            float2 cellPosition = cell + rand2dTo2d(cell);
            float2 toCell = cellPosition - value;
            float distToCell = length(toCell);
            if (distToCell < minDistToCell)
            {
                minDistToCell = distToCell;
                closestCell = cell;
            }
        }
    }
    float random = rand2dTo1d(closestCell);
    return float2(minDistToCell, random);
}

float2 VoronoiNoise(float3 value)
{
    float3 baseCell = floor(value);
    float minDistToCell = 10;
    float3 closestCell;
    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int z = -1; z <= 1; z++)
            {
                float3 cell = baseCell + float3(x, y, z);
                float3 cellPosition = cell + rand3dTo3d(cell);
                float3 toCell = cellPosition - value;
                float distToCell = length(toCell);
                if (distToCell < minDistToCell)
                {
                    minDistToCell = distToCell;
                    closestCell = cell;
                }
            }
        }
    }
    float random = rand3dTo1d(closestCell);
    return float2(minDistToCell, random);
}

float3 VoronoiNoiseWithBorder(float2 value)
{
    float2 baseCell = floor(value);
    //first pass to find the closest cell
    float minDistToCell = 10;
    float2 toClosestCell;
    float2 closestCell;
    [unroll]
    for (int x1 = -1; x1 <= 1; x1++) 
    {
        [unroll]
        for (int y1 = -1; y1 <= 1; y1++) 
        {
            float2 cell = baseCell + float2(x1, y1);
            float2 cellPosition = cell + rand2dTo2d(cell);
            float2 toCell = cellPosition - value;
            float distToCell = length(toCell);
            if (distToCell < minDistToCell) 
            {
                minDistToCell = distToCell;
                closestCell = cell;
                toClosestCell = toCell;
            }
        }
    }
    //second pass to find the distance to the closest edge
    float minEdgeDistance = 10;
    [unroll]
    for (int x2 = -1; x2 <= 1; x2++) 
    {
        [unroll]
        for (int y2 = -1; y2 <= 1; y2++) 
        {
            float2 cell = baseCell + float2(x2, y2);
            float2 cellPosition = cell + rand2dTo2d(cell);
            float2 toCell = cellPosition - value;

            float2 diffToClosestCell = abs(closestCell - cell);
            bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y < 0.1;
            if (!isClosestCell) 
            {
                float2 toCenter = (toClosestCell + toCell) * 0.5;
                float2 cellDifference = normalize(toCell - toClosestCell);
                float edgeDistance = dot(toCenter, cellDifference);
                minEdgeDistance = min(minEdgeDistance, edgeDistance);
            }
        }
    }
    float random = rand2dTo1d(closestCell);
    return float3(minDistToCell, random, minEdgeDistance);
}

float3 VoronoiNoiseWithBorder(float3 value)
{
    float3 baseCell = floor(value);
    //first pass to find the closest cell
    float minDistToCell = 10;
    float3 toClosestCell;
    float3 closestCell;
    [unroll]
    for (int x1 = -1; x1 <= 1; x1++)
    {
        [unroll]
        for (int y1 = -1; y1 <= 1; y1++)
        {
            [unroll]
            for (int z1 = -1; z1 <= 1; z1++)
            {
                float3 cell = baseCell + float3(x1, y1, z1);
                float3 cellPosition = cell + rand3dTo3d(cell);
                float3 toCell = cellPosition - value;
                float distToCell = length(toCell);
                if (distToCell < minDistToCell)
                {
                    minDistToCell = distToCell;
                    closestCell = cell;
                    toClosestCell = toCell;
                }
            }
        }
    }
    //second pass to find the distance to the closest edge
    float minEdgeDistance = 10;
    [unroll]
    for (int x2 = -1; x2 <= 1; x2++)
    {
        [unroll]
        for (int y2 = -1; y2 <= 1; y2++)
        {
            [unroll]
            for (int z2 = -1; z2 <= 1; z2++)
            {
                float3 cell = baseCell + float3(x2, y2, z2);
                float3 cellPosition = cell + rand3dTo3d(cell);
                float3 toCell = cellPosition - value;

                float3 diffToClosestCell = abs(closestCell - cell);
                bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y + diffToClosestCell.z < 0.1;
                if (!isClosestCell)
                {
                    float3 toCenter = (toClosestCell + toCell) * 0.5;
                    float3 cellDifference = normalize(toCell - toClosestCell);
                    float edgeDistance = dot(toCenter, cellDifference);
                    minEdgeDistance = min(minEdgeDistance, edgeDistance);
                }
            }
        }
    }
    float random = rand3dTo1d(closestCell);
    return float3(minDistToCell, random, minEdgeDistance);
}


//Tiling
inline float2 modulo(float2 divident, float2 divisor)
{
    float2 positiveDivident = divident % divisor + divisor;
    return positiveDivident % divisor;
}

inline float3 modulo(float3 divident, float3 divisor)
{
    float3 positiveDivident = divident % divisor + divisor;
    return positiveDivident % divisor;
}

float TilingPerlinNoise(float2 value, float2 period)
{
    float2 cellsMimimum = floor(value);
    float2 cellsMaximum = ceil(value);

    cellsMimimum = modulo(cellsMimimum, period);
    cellsMaximum = modulo(cellsMaximum, period);

    float2 lowerLeftDirection = rand2dTo2d(float2(cellsMimimum.x, cellsMimimum.y)) * 2 - 1;
    float2 lowerRightDirection = rand2dTo2d(float2(cellsMaximum.x, cellsMimimum.y)) * 2 - 1;
    float2 upperLeftDirection = rand2dTo2d(float2(cellsMimimum.x, cellsMaximum.y)) * 2 - 1;
    float2 upperRightDirection = rand2dTo2d(float2(cellsMaximum.x, cellsMaximum.y)) * 2 - 1;

    float2 fraction = frac(value);

    float lowerLeftFunctionValue = dot(lowerLeftDirection, fraction - float2(0, 0));
    float lowerRightFunctionValue = dot(lowerRightDirection, fraction - float2(1, 0));
    float upperLeftFunctionValue = dot(upperLeftDirection, fraction - float2(0, 1));
    float upperRightFunctionValue = dot(upperRightDirection, fraction - float2(1, 1));

    float interpolatorX = easeInOut(fraction.x);
    float interpolatorY = easeInOut(fraction.y);

    float lowerCells = lerp(lowerLeftFunctionValue, lowerRightFunctionValue, interpolatorX);
    float upperCells = lerp(upperLeftFunctionValue, upperRightFunctionValue, interpolatorX);

    float noise = lerp(lowerCells, upperCells, interpolatorY);
    return noise;
}

float TilingLayeredNoise(float2 value, float roughness, float persistance, float2 period)
{
    float noise = 0;
    float frequency = 1;
    float factor = 1;
    [unroll]
    for (int i = 0; i < OCTAVES; i++)
    {
        noise = noise + TilingPerlinNoise(value * frequency + i * 0.72354, period * frequency) * factor;
        factor *= persistance;
        frequency *= roughness;
    }
    return noise;
}

float3 TilingVoronoiNoiseWithBorder(float3 value, float3 period)
{
    float3 baseCell = floor(value);
    //first pass to find the closest cell
    float minDistToCell = 10;
    float3 toClosestCell;
    float3 closestCell;
    [unroll]
    for (int x1 = -1; x1 <= 1; x1++)
    {
        [unroll]
        for (int y1 = -1; y1 <= 1; y1++)
        {
            [unroll]
            for (int z1 = -1; z1 <= 1; z1++)
            {
                float3 cell = baseCell + float3(x1, y1, z1);
                float3 tiledCell = modulo(cell, period);
                float3 cellPosition = cell + rand3dTo3d(tiledCell);
                float3 toCell = cellPosition - value;
                float distToCell = length(toCell);
                if (distToCell < minDistToCell)
                {
                    minDistToCell = distToCell;
                    closestCell = cell;
                    toClosestCell = toCell;
                }
            }
        }
    }
    //second pass to find the distance to the closest edge
    float minEdgeDistance = 10;
    [unroll]
    for (int x2 = -1; x2 <= 1; x2++)
    {
        [unroll]
        for (int y2 = -1; y2 <= 1; y2++)
        {
            [unroll]
            for (int z2 = -1; z2 <= 1; z2++)
            {
                float3 cell = baseCell + float3(x2, y2, z2);
                float3 tiledCell = modulo(cell, period);
                float3 cellPosition = cell + rand3dTo3d(tiledCell);
                float3 toCell = cellPosition - value;

                float3 diffToClosestCell = abs(closestCell - cell);
                bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y + diffToClosestCell.z < 0.1;
                if (!isClosestCell)
                {
                    float3 toCenter = (toClosestCell + toCell) * 0.5;
                    float3 cellDifference = normalize(toCell - toClosestCell);
                    float edgeDistance = dot(toCenter, cellDifference);
                    minEdgeDistance = min(minEdgeDistance, edgeDistance);
                }
            }
        }
    }
    float random = rand3dTo1d(closestCell);
    return float3(minDistToCell, random, minEdgeDistance);
}

#endif