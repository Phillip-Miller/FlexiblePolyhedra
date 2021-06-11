import bpy
from math import *
#@Author Phillip M 6/9/2021
#Created blender script to make precise triangles given coordinates to allow for custom angles and whatnot
width = .5
#Triangles Labled A,B,C,D Respectively
#verts = [[0,0,0], [17,0,0], [12*cos(0.783662049 ),12*sin(0.783662049),0]]
#verts = [[0,0,0], [10,0,0], [10*cos(0.988432089),10*sin(0.988432089),0]]
#verts = [[0,0,0], [11,0,0], [12*cos(1.09467727),-1*12.0*sin(1.09467727),0]]
verts = [[0,0,0], [12,0,0], [5*cos(0.958192179),5*sin(0.958192179),0]]

for x in range(3): #Reusable way of changing width if needed
    addWidth = verts[x].copy()
    addWidth[2]+= width
    verts.append(addWidth)
edges = []

faces = [[0,1,2],[3,4,5],[0,1,4,3],[1,2,5,4],[0,2,5,3]]
#Need to make sure you go clockwise/counter clock wise for shapes with more than 3 verticies




#mod_skin = obj.modifiers.new('Skin','SKIN')

name = "New Object"
mesh = bpy.data.meshes.new(name)
obj = bpy.data.objects.new(name,mesh)
col = bpy.data.collections.get("Collection")
col.objects.link(obj)
bpy.context.view_layer.objects.active = obj
mesh.from_pydata(verts,edges,faces)
#maybe look into using bMesh
#Values calculated via SSS calculator, created orientation matching printed sheet
#[0,0,0], [17,0,0], [12*cos(0.783662049 ),12*sin(0.783662049),0] #Triangle A (12,12,17)
#[0,0,0], [10,0,0], [10*cos(0.0.988432089 ),10*sin(0.0.988432089),0] #Triangle B (10,10,11)
#[0,0,0], [11,0,0], [12*cos(1.09467727),-1*12.0*sin(1.09467727),0] #Triangle C (11,12,12)
#[0,0,0], [12,0,0], [5*cos(0.958192179),5*sin(0.958192179),0] #Triangle D (rotated to be flat) (5,10,12)
