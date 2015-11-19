SimpleCloth
===========
**VVVV node set for a moduluar cloth simulation system**

quickly hacked together for the visuals acompaning the [node15 symposium](http://node15.vvvv.org/program/wrapped-in-code-the-informed-body-symposium), frankfurt

by no means physically correct! 

v 1.0:
* CreateCloth: init cloth plane
* GlobalForce: cloth properties, world force
* Attractor: spherical distortion
* LocalForce: force per vertex
* Pin: pin vertex to current position
* SetPosition: try set vertex position
* SetTarget: vertex target
* Evaluate: evaluate system
* Split: output all vertices
* Sample: get position on cloth plane
* AsTexture: output data as texture