Writing a set of instructions
=============================

When providing a navigation service to users, they are presented with a route.
Oftentimes, the users also want to have turn-by-turn instructions. Itinero has support to flexibly generate instructions.

This document is a guide on how to extend or how to translate these instructions

Using the API
-------------

With the default, builtin instructions, adding instructions is really easy

```
using Itinero.Instructions;

...

Route route = ...

// Add english instructions to the route, under the tag `maneuver`
route = route.WithInstructions().MergeInstructions("maneuver");

```

To have more advanced instructions, first calculate a `Route`-object, then,

```
using Itinero.Instructions;

...


// Load the instruction configuration - the config file itself is described later in this document
InstructionsGenerator instructions = InstructionGenerator.FromConfigFile("path to configuration.json");

Route route = ...

// And apply them...
route.WithInstructions(instructions)
     .MergeInstructions("maneuver", "en"); // Add a tag `maneuver` which contains the english text
```

Context: how are instructions generated?
----------------------------------------

1. The route is calculated and stored in a `Route`-object. Note that the `Route`-object also has some metadata on branches (thus: the streets not taken but which are passed)
2. Itinero calculates the most appropriate `Instruction`s for the route
3. Every instruction is translated into text
4. The route object is amended with those instructions

Determining the most appropriate instruction
--------------------------------------------

### Instruction generators

Itinero has a list of `InstructionGenerators` available. An `InstructionGenerator`-is a factory-object which, given a `Route` and a point at the route, the instructionGenerator will attempt to emit the respective instruction.

For example, given a route going via street *A* (at index 0), then go via roundabout *B* (at index 1) to continue over road *C*. Road *C* is very long and has thus multiple segments: index 2, 3 and 4.

The `RoundaboutInstructionGenerator` will attempt to emit a `RoundaboutInstruction`. If the `RoundaboutInstructionGenerator` is given the route at index 1, it'll happily emit a `RoundaboutInstruction` (which contains all kind of extra data, such as the absolute degrees turned; the number of the exit taken, ...). Given the route above at any other index, it'll wont return anything as it doesn't find a roundabout there.

The `BaseInstructionGenerator` on the other hand isn't picky at all and will _always_ return a very simple `BaseInstruction`, which simply knows if the traveller is turning left or right.

In other words, there are some highly specialized instruction generators which are only applicable in some very specific cases, whereas the more general ones can be used (nearly always).

### Builtin generators

The following instruction generators are builtin:

- `BaseInstruction` which simply contains turnDegrees for one segment
segments of the route
- `StartInstruction` to indicate in what direction one has to start. This instruction contains an _embedded_ instruction object named `then` to 
- `EndInstruction` to indicate you have reached your destination
- `IntersectionInstruction` to indicate one has to cross a road (currently in beta)
- `FollowAlong` which detects a straight part of the same road - might use multiple 
- `FollowBend` which detects a turn in a single road
- `RoundaboutInstruction` to indicate that one has to take a roundabout


### Determining the instructions

In order to emit the instructions, Itinero will travel along the route virtually:

1. It will start at the first point of the route
2. The instruction generators will be tried one by one, from most specialized one to least specialized one*. The first instruction emited will be used
3. The route will be travelled till after the emitted instruction
4. A new instruction is emitted as in step 2
5. Repeat from step 3. until the end of the route

*: The order of instruction generators is actually defined in the config file. For the default instructions, this order is the order of generators above.

### Adding a custom generator

To add your own generator for a new type of instruction:

- Make a new class which extends `BaseInstruction`, add your needed metadata as **public properties**. It is important that they are public properties as they are picked up with reflection to create the texts.
- Make a new class which extends `InstructionsGenerator` and emits your instruction when applicable.
- Generate the `InstructionsGenerator` with `InstructionsGenerator instructions = InstructionGenerator.FromConfigFile("path to configuration.json", new MyGeneratorInstructionGenerator(), ...);`


The config file
---------------

The config file is a [.JSON](https://en.wikipedia.org/wiki/JSON)-file. It contain the following fields:

- The documentation fields `name`, `description`
- `generators`: a list of (names of) instructions that have to be emitted, the most specialized one first, e.g.  `"generators": ["roundabout", "followbend", "followalong", "end", "base" ]`
- `languages`, which contains the mapping from instruction to text. The top level contains a language code key followed by the actual mapping, e.g. `"languages": { "en": { ... actual mapping ... } }`

Mapping instructions to text
----------------------------

The _actual mapping_ is an object. The `key` of an object is a __condition to follow__, whereas the value is either another mapping or a string which is substituted.

### Conditions

The condition to follow can be a type of instruction or a value that should be followed. For example, the mapping could look as:

```
"languages:{
  "en": -- the language key
   { -- the actual mapping only starts here:
   
   -- the condition, in this case: the instruction must be a 'base' instruction'
   "base": "Do something basic"
   
   "roundabout": "Take the rounabout
   
   -- "*" acts as the fallback-condition; if no other condition matches this one is taken
   "*": "Do something else"
  }
}

```

### Substitutions

Texts can also contain properties, for example:

```
 "base":
 "Turn $turnDegrees on $.name"
```

In this case, the property `turnDegrees` will be added in the text. To access properties of the OSM-segment at the start of the relation, one can use `$.key`, in the example the name of the street is added. To access the tags of the next OSM-segment (the one _after_ this instruction), use `$+key`, for the _previous_ segment use `$-key`


### Advanced conditions

Of course, the name isn't always set. For this, one can check this in the condition:

```
  "$.name": "Follow along on $.name"
```

When a substitution is used in a condition, it means _if this property is available_

These instructions can be combined, e.g.

```
"languages": {"en":{

  "base": {
  	"$.name": "Follow along on $.name",
  	"*": "Follow along
  },
  "*": "Do something else"
}}
```

One can also add equations:

```
"base": {
  "$turndegrees<0": "Turn right",
  "$turndegrees>0": "Turn left",
  "$turndegrees=0": "Go straight on
}

```
and logical operators:

```
"base": {
  "$turndegrees>15&$turndegrees<35": "Turn slightly left"
}
```


### Extensions

As can be seen above, the above format quickly becomes cumbersome - for example to go left over `followBend` and `followAlong`:


```
"en": {
 "base": {
  "$turndegrees<0": "Turn right",
  "$turndegrees>0": "Turn right",
  "$turndegrees=0": "Go straight on
},
"followBend": {
  "$turndegrees<0": "Follow the bend to the right",
  "$turndegrees>0": "Follow the bend to the left",
  "$turndegrees=0": "Go straight on
},
"roundabout": {
  "$turndegrees<0": {
  	"$exitNumber=1": "Go left by taking the first exit",
  	"$exitNumber=2": "Go left by taking the second exit",
  	...
  }
  "$turndegrees>0": { ...  } ,
  "$turndegrees=0": { ... }
} 
}

```

What if we want to add for example a new type of turn, e.g. `turn sharply left' and `turn slightly left`? This would explode the configuration file.

For this, it is possible to __extend__ the instructions in order to reuse a mapping. A mapping can be added to the special mapping `extensions` which can then be reused as if it were a native property:

```
"en":{
  "extensions": {
    "lr": {
      "$turnDegrees<0": "left",
      "$turnDegrees>0": "right",
    }
  },
  "base": {
    "$.name": "Go $lr on $.name",
    "*": "Go $lr"
  },
  "followBend": "Follow the bend $lr",
  "roundabout": {
  	"$exitNumber=1": "Go $lr by taking the first exit",
  	"$exitNumber=2": "Go $lr by taking the second exit",
  	...
  }
}
```

Adding a new type of left/right can now be done in a single place, and is applied everywhere where `$lr` is used.




