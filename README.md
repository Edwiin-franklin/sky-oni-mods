# Sky's Oxygen Not Included mods

The source code for my Oxygen Not Included mods.

Many thanks to Peter Han and Cairath for their help and also for graciously letting me reference their code. I wouldn't have been able to mod ONI without their help. In particular, the build system used in this solution was originally written by Cairath.

## Issues

If you find what you think is a bug, please feel free to open a report in [Issues](https://github.com/skairunner/sky-oni-mods/issues). If you do not include steps for reproduction, your issue will likely not be acted on.

## Mods

### Bleachstone Refinery

Adds a new building that refines Chlorine into Bleachstone. Unlocked with Catalytics research, and takes the same materials to construct as the Oxylite Refinery but uses half the power (600 W).

600 g/s Chlorine + 3 g/s Copper = 600 g/s Bleachstone.

### Drain

Adds a new building to the Plumbing tab called the Drain, built with 100kg of metal. It passively pumps liquids inside it into pipes at a rate of 100g/s, and is thus useful for buildings that "dribble" liquid like the Natural Gas Generator, or for cleaning up Algae Terrarium automation.

### Drywall Hides Pipes

Drywall and Tempshift Plate hides pipes, conduits and conveyor belts.

### Expanded Lights

Add new lights that are much more convenient for lighting areas: Floodlight, Tile Light, LED Light. Additionally, Ceiling Light and Lamp have smooth lighting, and produce less heat.

#### New lights

**Floodlight**: Rotatable. An industrial light that consumes 30W and illuminates a 5 x 16 rectangular at a brightness of 6000 Lux (no falloff) at the cost of a heavy decor penalty. Costs 100 kg of refined metal, and is unlocked with Low-Resistance Conductors.

**LED Light**: A high-tech light that produces no heat, consuming 2W to light a circular area. Can be built anywhere. Costs 50 kg of refined metal and 50 kg of plastic, and is unlocked with Catalytics.

**Tile Light**: Rotatable. A tiny light that can be embedded in other tiles, consuming 5W and illuminating a similar area and brightness as a Ceiling Light. Is a background layer building, meaning it can be built within tile. Costs 50 kg of plastic, and is unlocked with Artistic Expression.

#### Tweaked lights

**Ceiling Light**: Produces 100 DTU/s (down from 500 DTU/s).

**Lamp**: Consumes 4W (down from 8W) and produces 100 DTU/s (down from 500 DTU/s).


### Radiate Heat In Space

Add mechanic where select buildings passively radiate heat when in space. The radiated amount of heat scales by the fourth power of the temperature of the building. This allows buildings to be potentially operated in space without a "drop of liquid" cooling system, although many buildings will have to be periodically disabled, because they do not radiate enough heat to keep themselves cool while continuously operating.

#### Approximate net heat production at 20C

Automatic Dispenser: -160 DTU/s  
Auto-Sweeper: 1200 DTU/s*  
Battery: 550 DTU/s☨  
Battery, Jumbo: -100 DTU/s  
Battery, Smart: -600 DTU/s 
Ceiling Light: 400 DTU/s  
Conveyor Loader: 1600 DTU/s**  
Duplicant Checkpoint: 450 DTU/s☨☨  
Gantry: 1500 DTU/s***  
Lamp: 300 DTU/s
Power Transformer: 500 DTU/s  
Power Transformer, Large: 500 DTU/s  
Robo-Miner: 700 DTU/s****  
Smart Storage Bin: -120 DTU/s  

\* Radiates 800 DTU/s even if inactive  
\*\* Radiates 400 DTU/s even if inactive  
\*\*\* Radiates 500 DTU/s even if inactive  
\*\*\*\* Radiates 1300 DTU/s even if inactive  
☨ Battery has a net positive heat produced even though it produces less than the other batteries, because its surface area is smaller.  
☨☨ Duplicant Checkpoint has a tiny surface area and is also shiny, and thus radiates very little.

### Radiator

Adds a new building to the Utilities tab called the Radiator, built with 400kg of Refined Metal. It is passively warmed up by liquid piped through it, and when it is in space (no backwall!), it passively cools itself by radiating its heat to space. It radiates more heat the hotter it is. Unlike the thermo-tuners, it does not take power but also does not actively pump heat. The Radiator does not overheat or be entombed, but it is damageable by meteorites. The Radiator also does not need access to the sun. It is unlocked through the research Temperature Modulation.

Aside from radiating heat into space, the Radiator can also be used as a more compact version of a typical radiant pipe loop. Simply plop a few in the fluid of your choice and route liquid through it!

### Raincheck (Printing Pod time refund)

Lets you take a raincheck on the Printing Pod. When you reject all items, it recharges faster, defaulting to 66% refunded.

### Storage Pod

Adds a new storage building, the Storage Pod. Costs 100 kg of Refined Metal and can be built anywhere. Stores 5t, and is unlocked with Refined Renovations.