/***********************************************************
 * Copyright (C) 2019 6degrees.xyz Inc.
 *
 * This file is part of the 6D.ai Beta SDK.
 ***********************************************************/

#include <metal_stdlib>
using namespace metal;

typedef struct {
    float2 position;
    float2 textureCoordinates;
} InputVertex;

typedef struct {
    float4 position [[ position ]];
    float2 textureCoordinates;
} FragVertex;

vertex FragVertex simpleVertex(unsigned int vertex_id [[ vertex_id ]],
                               constant InputVertex *vertices [[ buffer(0) ]]) {
    FragVertex outVertex;
    outVertex.position = float4(vertices[vertex_id].position, 0.0f, 1.0f);
    outVertex.textureCoordinates = vertices[vertex_id].textureCoordinates;
    return outVertex;
}

fragment half4 simpleTexture(FragVertex inVertex [[ stage_in ]],
                             texture2d<float, access::sample> texture [[ texture(0) ]]) {
    constexpr sampler s(address::clamp_to_edge, filter::linear);
    
    return half4(texture.sample(s, inVertex.textureCoordinates));
}

