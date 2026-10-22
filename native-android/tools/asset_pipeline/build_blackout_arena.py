#!/usr/bin/env python3
"""Build the Derby Arena BLACKOUT RUN industrial spillway arena (GLB 2.0)."""
import os, math, json, random, sys
sys.path.insert(0, os.path.dirname(__file__))
from glb_writer import GlbDocument, MeshBuilder, rot_y
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'app', 'src', 'main', 'assets', 'models', 'arena')
os.makedirs(OUT, exist_ok=True)
random.seed(121)
doc = GlbDocument()
concrete = doc.add_material('Floodway_Concrete', (0.24,0.27,0.28,1), 0.12, 0.92)
wet = doc.add_material('Wet_Asphalt', (0.075,0.09,0.10,1), 0.32, 0.36)
steel = doc.add_material('Oxidized_Steel', (0.18,0.21,0.22,1), 0.82, 0.58)
rust = doc.add_material('Burnt_Rust', (0.32,0.12,0.055,1), 0.58, 0.72)
yellow = doc.add_material('Hazard_Yellow', (0.96,0.61,0.07,1), 0.4, 0.48)
black = doc.add_material('Hazard_Black', (0.018,0.021,0.023,1), 0.18, 0.75)
water = doc.add_material('Oil_Slick', (0.035,0.08,0.09,1), 0.72, 0.16)
redglow = doc.add_material('Red_Beacon', (1,0.07,0.025,1), 0.05, 0.25, emissive=(1,0.035,0.01), emissive_strength=7)
cyanglow = doc.add_material('Core_Cyan', (0.02,0.85,0.9,1), 0.12, 0.22, emissive=(0.01,0.7,1), emissive_strength=5)
# ground and damaged industrial spillway
floor=MeshBuilder(); floor.disk(wet,(0,0,0),31.7,96,uv_scale=0.22)
for i in range(26):
    a=random.random()*math.tau; r=random.uniform(4,25)
    floor.disk(water,(math.cos(a)*r,0.018,math.sin(a)*r),random.uniform(.45,2.2),20,uv_scale=.7)
# long lane markings, maintenance strips and drains
for z in (-18,-12,-6,0,6,12,18):
    floor.box(yellow,(0,0.025,z),(0.12,0.035,3.0),uv_scale=.5)
for x in (-24,24):
    floor.box(steel,(x,0.03,0),(0.42,0.06,48),uv_scale=.8)
doc.add_node('Ground',doc.add_mesh(floor,'Ground'))
# perimeter flood wall with concrete buttresses and hazard caps
wall=MeshBuilder(); layout=[]
for i in range(64):
    a=math.tau*(i+.5)/64; x=math.cos(a)*29.5; z=math.sin(a)*29.5
    wall.box(concrete,(x,1.75,z),(3.1,3.5,1.5),rot=rot_y(-math.degrees(a)+90),uv_scale=.7)
    wall.box(yellow,(x,3.56,z),(2.9,.14,1.55),rot=rot_y(-math.degrees(a)+90),uv_scale=.6)
    if i%4==0:
        wall.box(steel,(x,4.2,z),(0.42,1.4,0.48),rot=rot_y(-math.degrees(a)+90))
doc.add_node('Floodwall',doc.add_mesh(wall,'Floodwall'))
# catwalks, gantry supports, and overhead industrial pipes
rig=MeshBuilder()
for x in (-20,20):
    for z in (-17,0,17):
        rig.box(steel,(x,4.8,z),(0.8,9.6,0.8),uv_scale=.4)
        rig.box(rust,(x,9.7,z),(1.5,.45,1.5),uv_scale=.4)
for z in (-17,0,17):
    rig.box(steel,(0,9.3,z),(41,.55,.8),uv_scale=.4)
    rig.box(yellow,(0,8.98,z),(39,.08,.1),uv_scale=.4)
# massive drainage pipes along both sides
for x in (-25.2,25.2):
    rig.cylinder(rust,(x,2.0,0),1.15,1.15,44,32,axis='z',uv_scale=.35)
    for z in range(-20,22,5):
        rig.cylinder(steel,(x,2.0,z),1.28,1.28,.24,32,axis='z',uv_scale=.35)
# central turbine platform / raised broken ramps
rig.box(steel,(0,.32,-5),(8,.55,5),uv_scale=.5)
rig.box(yellow,(0,.62,-5),(6.8,.08,3.8),uv_scale=.4)
rig.box(concrete,(-8,.22,10),(5,.42,2.4),rot=rot_y(-12),uv_scale=.6)
rig.box(concrete,(8,.22,11),(5,.42,2.4),rot=rot_y(12),uv_scale=.6)
# four emergency beacon towers
for x,z in [(-20,-20),(20,-20),(-20,20),(20,20)]:
    rig.box(steel,(x,2.4,z),(1.4,4.8,1.4),uv_scale=.3)
    rig.cylinder(redglow,(x,5.1,z),.38,.38,.65,12,axis='y')
    rig.box(black,(x,5.48,z),(1.2,.12,1.2))
doc.add_node('GantryAndPipes',doc.add_mesh(rig,'GantryAndPipes'))
# wreckage piles and marked extraction pad
props=MeshBuilder()
for i in range(18):
    a=random.random()*math.tau; r=random.uniform(7,24); x=math.cos(a)*r; z=math.sin(a)*r
    if abs(x)<6 and -10<z<2: continue
    props.box(rust if i%3==0 else steel,(x,random.uniform(.18,.55),z),(random.uniform(.45,1.5),random.uniform(.35,.95),random.uniform(.5,1.7)),rot=rot_y(random.uniform(0,180)),uv_scale=.45)
    if i%3==0:
        props.cylinder(black,(x,.35,z),.48,.48,.55,12,axis='y')
    if i % 2 == 0 and math.hypot(x, z) < 24:
        layout.append({'type':'wreck','x':round(x/0.04,1),'y':round(z/0.04,1),'radius':24.0,'height':0.9})
# extraction pad near north side, visible yellow border and cyan beacon
props.box(black,(0,.08,-23),(9,.16,5),uv_scale=.4)
for x in (-4.5,4.5): props.box(yellow,(x,.18,-23),(.22,.12,5.2),uv_scale=.3)
for z in (-25.5,-20.5): props.box(yellow,(0,.18,z),(9.2,.12,.22),uv_scale=.3)
props.cylinder(cyanglow,(0,.38,-23),.8,.8,.55,20,axis='y')
# reactor cells: visible cyan industrial canisters at mission coordinates (game units × 0.04)
for cx, cz in [(-12.0, 2.4), (11.4, 3.8), (-1.4, -10.6)]:
    props.cylinder(black,(cx,.38,cz),.72,.72,.62,16,axis='y')
    props.cylinder(cyanglow,(cx,.86,cz),.34,.34,.42,16,axis='y')
    props.cylinder(steel,(cx,1.10,cz),.42,.42,.12,16,axis='y')
doc.add_node('WreckageAndExtractionPad',doc.add_mesh(props,'WreckageAndExtractionPad'))
# far industrial skyline, stacks and smokestacks
sky=MeshBuilder()
for i in range(48):
    a=math.tau*i/48; r=random.uniform(47,90); h=random.uniform(8,30)
    sky.box(concrete,(math.cos(a)*r,h/2,math.sin(a)*r),(random.uniform(3,8),h,random.uniform(3,8)),rot=rot_y(random.uniform(0,90)))
for x,z,h in [(-65,-30,38),(63,-48,48),(-48,66,32),(75,30,42)]:
    sky.cylinder(steel,(x,h/2,z),2.2,2.8,h,16,axis='y')
    sky.cylinder(redglow,(x,h+.3,z),.32,.32,.6,12,axis='y')
sky.disk(wet,(0,-.06,0),150,48,uv_scale=.04,inner=31.7)
doc.add_node('IndustrialHorizon',doc.add_mesh(sky,'IndustrialHorizon'))
size=doc.write(os.path.join(OUT,'arena01_colosseum.glb'))
with open(os.path.join(OUT,'arena01_layout.json'),'w') as f: json.dump({'playableRadius':700,'barrierRadius':720,'arena':'BLACKOUT_SPILLWAY','obstacles':layout},f,indent=1)
print(f'BLACKOUT SPILLWAY written: {size/1024:.1f} KB, {len(doc.meshes)} meshes, {len(layout)} gameplay colliders')
