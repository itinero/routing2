name = "bicycle.fast"
description = "The fastest route to your destination (Profile for a normal bicycle)"

-- The hierarchy of types that this vehicle is; mostly used to check access restrictions
vehicle_types = {"vehicle", "bicycle"}

--[[
Calculate the actual factor.forward and factor.backward for a segment with the given properties
]]
function factor(tags, result)

    -- Cleanup the relation tags to make them usable with this profile
    tags = remove_relation_prefix(tags, "fast")

    -- initialize the result table on the default values
    result.forward_speed = 0
    result.backward_speed = 0
    result.forward = 0
    result.backward = 0
    result.canstop = true
    result.attributes_to_keep = {} -- not actually used anymore, but the code generation still uses this


    local parameters = default_parameters()
    parameters.timeNeeded = 1
    parameters.leastSafetyPenalty = 2


    local oneway = bicycle_oneway(tags)
    tags.oneway = oneway
    -- An aspect describing oneway should give either 'both', 'against' or 'width'


    -- forward calculation. We set the meta tag '_direction' to 'width' to indicate that we are going forward. The other functions will pick this up
    tags["_direction"] = "with"
    local access_forward = head({legal_access_be(tags), bicycle_legal_access(tags)})
    if(oneway == "against") then
        -- no 'oneway=both' or 'oneway=with', so we can only go back over this segment
        -- we overwrite the 'access_forward'-value with no; whatever it was...
        access_forward = "no"
    end
    if(access_forward ~= nil and access_forward ~= "no" and access_forward ~= false) then
        tags.access = access_forward -- might be relevant, e.g. for 'access=dismount' for bicycles
        result.forward_speed = max({ferry_speed(tags), min({legal_maxspeed_be(tags), parameters["maxspeed"], multiply({parameters["defaultSpeed"], bicycle_speed_factor(tags)})})})
        tags.speed = result.forward_speed
        local priority = calculate_priority(parameters, tags, result, access_forward, oneway, result.forward_speed)
        if (priority <= 0) then
            result.forward_speed = 0
        else
            result.forward = 1 / priority
        end
    end

    -- backward calculation
    tags["_direction"] = "against" -- indicate the backward direction to priority calculation
    local access_backward = head({legal_access_be(tags), bicycle_legal_access(tags)})
    if(oneway == "with") then
        -- no 'oneway=both' or 'oneway=against', so we can only go forward over this segment
        -- we overwrite the 'access_forward'-value with no; whatever it was...
        access_backward = "no"
    end
    if(access_backward ~= nil and access_backward ~= "no" and access_backward ~= false) then
        tags.access = access_backward
        result.backward_speed = max({ferry_speed(tags), min({legal_maxspeed_be(tags), parameters["maxspeed"], multiply({parameters["defaultSpeed"], bicycle_speed_factor(tags)})})})
        tags.speed = result.backward_speed
        local priority = calculate_priority(parameters, tags, result, access_backward, oneway, result.backward_speed)
        if (priority <= 0) then
            result.backward_speed = 0
        else
            result.backward = 1 / priority
        end
    end
end

--[[
Generates the factor according to the priorities and the parameters for this behaviour
Note: 'result' is not actually used
]]
function calculate_priority(parameters, tags, result, access, oneway, speed)
    local distance = 1
    local priority =
    1 * speed +
            15 * clean_permission_score(tags) +
            2 * multiply({atleast(parameters["leastSafetyRequired"], bicycle_safety(tags, parameters), 0, -1, tags), speed})

    local scalingfactor
    scalingfactor = 1

    if (scalingfactor == nil) then
        scalingfactor = 1
    end


    return priority * scalingfactor
end

--[[ Function called by itinero2 on every turn restriction relation
 ]]
function turn_cost_factor(attributes, result)
    local parameters = default_parameters()
    parameters.timeNeeded = 1
    parameters.leastSafetyPenalty = 2

    local has_access
    has_access = nil

    if ( has_access == "no" or has_access == "false") then
        result.factor = -1
    else
        result.factor = nil

    end
    -- not known by the profile or invalid value - use the default implementation
    if (result.factor == nil) then
        result.factor = calculate_turn_cost_factor(attributes, vehicle_types)
    end
end


function default_parameters()
    local parameters = {}
    parameters.defaultSpeed = 15
    parameters.maxspeed = 30
    parameters.timeNeeded = 0
    parameters.distance = 0
    parameters.comfort = 0
    parameters.safety = 0
    parameters.operatorNetworkScore = 0
    parameters.networkOperator = {}
    parameters.cycleHighwayNetworkScore = 0
    parameters.nodeNetworkScore = 0
    parameters.bicycleNetworkScore = 0
    parameters.trespassingPenalty = 15
    parameters.leastSafetyRequired = 0.11
    parameters.leastSafetyPenalty = 0

    return parameters
end


--[[
Gives a few cases where a highway=* _can never_ be entered, e.g. highway=proposed, highway=razed, ... Will give null otherwise

Unit: yes|no
Created by 
Uses tags: anyways:construction, highway
Used parameters: 
Number of combintations: 3
Returns values: 
]]
function legal_access_be(tags, parameters)
    local r = nil
    local cond
    cond = must_match({"anyways:construction", "highway"}, stringToTags({
        ["anyways:construction"] = eq("yes", tags["anyways:construction"], tags["anyways:construction"]),
        highway = eq("construction", tags["highway"], tags["highway"])
    }), tags)

    if ( cond == true or cond == "yes" ) then
        r = nil
    else
        local cond0
        cond0 = must_match({"highway"}, stringToTags({
            highway = {
                proposed = "no",
                abandoned = "no",
                disused = "no",
                razed = "no",
                construction = "no"
            }
        }), tags)

        if ( cond0 == true or cond0 == "yes" ) then
            r = nil
        else
            if (tags["razed:highway"] ~= nil) then
                r = eq("no", tags["razed:highway"])

            end
            if (tags["demolished:highway"] ~= nil) then
                r = eq("no", tags["demolished:highway"])

            end
            if (tags["disused:highway"] ~= nil) then
                r = eq("no", tags["disused:highway"])

            end
            if (tags["abandoned:highway"] ~= nil) then
                r = eq("no", tags["abandoned:highway"])

            end
            if (tags["proposed:highway"] ~= nil) then
                r = eq("no", tags["proposed:highway"])

            end
            if (tags["highway"] ~= nil) then
                r = eq("no", tags["highway"])

            end

        end

    end


    return r
end

--[[
Gives, for each type of highway, whether or not a normal bicycle can enter legally.
Note that legal access is a bit 'grey' in the case of roads marked private and permissive, in which case these values are returned 

Unit: 'designated': Access is allowed and even specifically for bicycles
'yes': bicycles are allowed here
'permissive': bicycles are allowed here, but this might be a private road or service where usage is allowed, but could be retracted one day by the owner
'dismount': cycling here is not allowed, but walking with the bicycle is
'destination': cycling is allowed here, but only if truly necessary to reach the destination
'private': this is a private road, only go here if the destination is here
'no': do not cycle here
Created by 
Uses tags: area, access, highway, service, bicycle, anyways:bicycle, anyways:access, anyways:construction
Used parameters: 
Number of combintations: 58
Returns values: 
]]
function bicycle_legal_access(tags, parameters)
    local r = nil
    if (tags["highway"] ~= nil) then
        local v
        v = tags["highway"]

        if (v == "cycleway") then
            r = "designated"
        elseif (v == "residential") then
            r = "yes"
        elseif (v == "living_street") then
            r = "yes"
        elseif (v == "service") then
            r = "yes"
        elseif (v == "services") then
            r = "yes"
        elseif (v == "track") then
            r = "yes"
        elseif (v == "crossing") then
            r = "dismount"
        elseif (v == "footway") then
            r = "dismount"
        elseif (v == "pedestrian") then
            r = "dismount"
        elseif (v == "corridor") then
            r = "dismount"
        elseif (v == "construction") then
            r = "dismount"
        elseif (v == "steps") then
            r = "dismount"
        elseif (v == "path") then
            r = "yes"
        elseif (v == "primary") then
            r = "yes"
        elseif (v == "primary_link") then
            r = "yes"
        elseif (v == "secondary") then
            r = "yes"
        elseif (v == "secondary_link") then
            r = "yes"
        elseif (v == "tertiary") then
            r = "yes"
        elseif (v == "tertiary_link") then
            r = "yes"
        elseif (v == "unclassified") then
            r = "yes"
        elseif (v == "road") then
            r = "yes"
        elseif (v == "trunk") then
            r = "no"
        elseif (v == "trunk_link") then
            r = "no"
        end
    end
    if (tags["service"] ~= nil) then
        local v0
        v0 = tags["service"]

        if (v0 == "parking_aisle") then
            r = "permissive"
        elseif (v0 == "driveway") then
            r = "private"
        elseif (v0 == "alley") then
            r = "yes"
        elseif (v0 == "bus") then
            r = "no"
        end
    end
    if (tags["access"] ~= nil) then
        local v1
        v1 = tags["access"]

        if (v1 == "no") then
            r = "no"
        elseif (v1 == "customers") then
            r = "private"
        elseif (v1 == "private") then
            r = "private"
        elseif (v1 == "permissive") then
            r = "permissive"
        elseif (v1 == "destination") then
            r = "yes"
        elseif (v1 == "delivery") then
            r = "destination"
        elseif (v1 == "service") then
            r = "destination"
        elseif (v1 == "permit") then
            r = "destination"
        end
    end
    if (tags["bicycle"] ~= nil) then
        local v2
        v2 = tags["bicycle"]

        if (v2 == "yes") then
            r = "yes"
        elseif (v2 == "no") then
            r = "no"
        elseif (v2 == "use_sidepath") then
            r = "no"
        elseif (v2 == "designated") then
            r = "designated"
        elseif (v2 == "permissive") then
            r = "permissive"
        elseif (v2 == "private") then
            r = "private"
        elseif (v2 == "official") then
            r = "designated"
        elseif (v2 == "dismount") then
            r = "dismount"
        elseif (v2 == "permit") then
            r = "destination"
        end
    end
    if (tags["anyways:construction"] ~= nil) then
        local v3
        v3 = tags["anyways:construction"]

        if (v3 == "yes") then
            r = "no"
        end
    end
    if (tags["anyways:access"] ~= nil) then
        local v4
        v4 = tags["anyways:access"]

        if (v4 == "no") then
            r = "no"
        elseif (v4 == "destination") then
            r = "destination"
        elseif (v4 == "yes") then
            r = "yes"
        end
    end
    if (tags["anyways:bicycle"] ~= nil) then
        r = tags["anyways:bicycle"]

    end
    if (tags["area"] ~= nil) then
        local v5
        v5 = tags["area"]

        if (v5 == "yes") then
            r = "no"
        end
    end


    if (r == nil) then
        r = "no"
    end


    return r
end

--[[
Determines wether or not a bicycle can go in both ways in this street, and if it is oneway, in what direction

Unit: both: direction is allowed in both direction
with: this is a oneway street with direction allowed with the grain of the way
against: oneway street with direction against the way
Created by 
Uses tags: oneway, anyways:oneway, anyways:oneway:bicycle, oneway:bicycle, junction, cycleway, cycleway:left:oneway, cycleway:right:oneway, cycleway:left
Used parameters: 
Number of combintations: 44
Returns values: 
]]
function bicycle_oneway(tags, parameters)
    local r = nil
    if (tags["oneway"] ~= nil) then
        local v6
        v6 = tags["oneway"]

        if (v6 == "yes") then
            r = "with"
        elseif (v6 == "no") then
            r = "both"
        elseif (v6 == "1") then
            r = "with"
        elseif (v6 == "-1") then
            r = "against"
        end
    end
    if (tags["cycleway:left"] ~= nil) then
        local v7
        v7 = tags["cycleway:left"]

        if (v7 == "yes") then
            r = "both"
        elseif (v7 == "lane") then
            r = "both"
        elseif (v7 == "track") then
            r = "both"
        elseif (v7 == "shared_lane") then
            r = "both"
        elseif (v7 == "share_busway") then
            r = "both"
        elseif (v7 == "opposite_lane") then
            r = "both"
        elseif (v7 == "opposite_track") then
            r = "both"
        elseif (v7 == "opposite") then
            r = "both"
        end
    end
    if (tags["cycleway"] ~= nil) then
        local v8
        v8 = tags["cycleway"]

        if (v8 == "right") then
            r = "against"
        elseif (v8 == "opposite_lane") then
            r = "both"
        elseif (v8 == "track") then
            r = "both"
        elseif (v8 == "lane") then
            r = "both"
        elseif (v8 == "opposite") then
            r = "both"
        elseif (v8 == "opposite_share_busway") then
            r = "both"
        elseif (v8 == "opposite_track") then
            r = "both"
        end
    end
    if (tags["junction"] ~= nil) then
        local v9
        v9 = tags["junction"]

        if (v9 == "roundabout") then
            r = "with"
        end
    end
    if (tags["cycleway:left:oneway"] ~= nil) then
        local v10
        v10 = tags["cycleway:left:oneway"]

        if (v10 == "no") then
            r = "both"
        end
    end
    if (tags["cycleway:right:oneway"] ~= nil) then
        local v11
        v11 = tags["cycleway:right:oneway"]

        if (v11 == "no") then
            r = "both"
        end
    end
    if (tags["oneway:bicycle"] ~= nil) then
        local v12
        v12 = tags["oneway:bicycle"]

        if (v12 == "yes") then
            r = "with"
        elseif (v12 == "no") then
            r = "both"
        elseif (v12 == "1") then
            r = "with"
        elseif (v12 == "-1") then
            r = "against"
        end
    end
    if (tags["anyways:oneway:bicycle"] ~= nil) then
        local v13
        v13 = tags["anyways:oneway:bicycle"]

        if (v13 == "yes") then
            r = "with"
        elseif (v13 == "no") then
            r = "both"
        elseif (v13 == "1") then
            r = "with"
        elseif (v13 == "-1") then
            r = "against"
        end
    end
    if (tags["anyways:oneway"] ~= nil) then
        local v14
        v14 = tags["anyways:oneway"]

        if (v14 == "yes") then
            r = "with"
        elseif (v14 == "no") then
            r = "both"
        elseif (v14 == "1") then
            r = "with"
        elseif (v14 == "-1") then
            r = "against"
        end
    end


    if (r == nil) then
        r = "both"
    end


    return r
end

--[[
Gives the expected speed for a ferry. This includes the time needed to board and the expected waiting time (if duration is present). This uses the tag '_length', which is expected to be added by the preprocessing-step.

Unit: km/h
Created by 
Uses tags: route
Used parameters: 
Number of combintations: 3
Returns values: 
]]
function ferry_speed(tags, parameters)
    local r = nil
    local cond1
    local value = tags["route"]
    local v15
    v15 = value

    if (v15 == "ferry") then
        cond1 = "yes"
    end

    if ( cond1 == true or cond1 == "yes" ) then
        r = nil
        local m
        m = nil
        m = 100


        if (m ~= nil) then
            if ( r == nil or r > m ) then
                r = m
            end
        end
        m = nil
        m = head({head(stringToTags({
            ["_speed"] = parse(tags["_speed"])
        }, tags)), multiply({0.06, if_then_else(is_null(head(stringToTags({
            ["_length"] = parse(tags["_length"])
        }, tags))), 83.33333333333333, head(stringToTags({
            ["_length"] = parse(tags["_length"])
        }, tags))), inv(sum({head(stringToTags({
            duration = parse(tags["duration"])
        }, tags)), multiply({0.2, if_then_else(is_null(head(stringToTags({
            interval = parse(tags["interval"])
        }, tags))), 20, head(stringToTags({
            interval = parse(tags["interval"])
        }, tags)))})}))})})


        if (m ~= nil) then
            if ( r == nil or r > m ) then
                r = m
            end
        end

    end


    return r
end

--[[
Gives, for each type of highway, which the default legal maxspeed is in Belgium. This file is intended to be reused for in all vehicles, from pedestrian to car. In some cases, a legal maxspeed is not really defined (e.g. on footways). In that case, a socially acceptable speed should be taken (e.g.: a bicycle on a pedestrian path will go say around 12km/h)

Unit: km/h
Created by 
Uses tags: maxspeed, highway, designation
Used parameters: 
Number of combintations: 29
Returns values: 
]]
function legal_maxspeed_be(tags, parameters)
    local r = nil
    if (tags["highway"] ~= nil) then
        local v16
        v16 = tags["highway"]

        if (v16 == "cycleway") then
            r = 30
        elseif (v16 == "footway") then
            r = 20
        elseif (v16 == "crossing") then
            r = 20
        elseif (v16 == "pedestrian") then
            r = 15
        elseif (v16 == "path") then
            r = 15
        elseif (v16 == "corridor") then
            r = 5
        elseif (v16 == "residential") then
            r = 30
        elseif (v16 == "living_street") then
            r = 20
        elseif (v16 == "service") then
            r = 30
        elseif (v16 == "services") then
            r = 30
        elseif (v16 == "track") then
            r = 20
        elseif (v16 == "unclassified") then
            r = 50
        elseif (v16 == "road") then
            r = 50
        elseif (v16 == "motorway") then
            r = 120
        elseif (v16 == "motorway_link") then
            r = 120
        elseif (v16 == "trunk") then
            r = 90
        elseif (v16 == "trunk_link") then
            r = 90
        elseif (v16 == "primary") then
            r = 90
        elseif (v16 == "primary_link") then
            r = 90
        elseif (v16 == "secondary") then
            r = 70
        elseif (v16 == "secondary_link") then
            r = 70
        elseif (v16 == "tertiary") then
            r = 50
        elseif (v16 == "tertiary_link") then
            r = 50
        elseif (v16 == "construction") then
            r = 10
        end
    end
    if (tags["designation"] ~= nil) then
        local v17
        v17 = tags["designation"]

        if (v17 == "towpath") then
            r = 30
        end
    end
    if (tags["maxspeed"] ~= nil) then
        r = parse(tags["maxspeed"])

    end


    if (r == nil) then
        r = 30
    end


    return r
end

--[[
Calculates a speed factor for bicycles based on physical features, e.g. a sand surface will slow a cyclist down; going over pedestrian areas even more, ...

Unit: 
Created by 
Uses tags: access, highway, ramp:bicycle, surface, tracktype, incline
Used parameters: 
Number of combintations: 53
Returns values: 
]]
function bicycle_speed_factor(tags, parameters)
    local r = nil
    r = 1
    local m0 = nil
    if (tags["access"] ~= nil) then
        m0 = nil
        local v18
        v18 = tags["access"]

        if (v18 == "dismount") then
            m0 = 0.15
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end
    if (tags["highway"] ~= nil) then
        m0 = nil
        local v19
        v19 = tags["highway"]

        if (v19 == "path") then
            m0 = 0.5
        elseif (v19 == "track") then
            m0 = 0.7
        elseif (v19 == "construction") then
            m0 = 0.5
        elseif (v19 == "steps") then
            m0 = 0.1
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end
    if (tags["ramp:bicycle"] ~= nil) then
        m0 = nil
        local v20
        v20 = tags["ramp:bicycle"]

        if (v20 == "yes") then
            m0 = 3
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end
    if (tags["surface"] ~= nil) then
        m0 = nil
        local v21
        v21 = tags["surface"]

        if (v21 == "paved") then
            m0 = 0.99
        elseif (v21 == "asphalt") then
            m0 = 1
        elseif (v21 == "concrete") then
            m0 = 1
        elseif (v21 == "metal") then
            m0 = 1
        elseif (v21 == "wood") then
            m0 = 1
        elseif (v21 == "concrete:lanes") then
            m0 = 0.95
        elseif (v21 == "concrete:plates") then
            m0 = 1
        elseif (v21 == "paving_stones") then
            m0 = 1
        elseif (v21 == "sett") then
            m0 = 0.9
        elseif (v21 == "unhewn_cobblestone") then
            m0 = 0.75
        elseif (v21 == "cobblestone") then
            m0 = 0.8
        elseif (v21 == "unpaved") then
            m0 = 0.75
        elseif (v21 == "compacted") then
            m0 = 0.99
        elseif (v21 == "fine_gravel") then
            m0 = 0.99
        elseif (v21 == "gravel") then
            m0 = 0.9
        elseif (v21 == "dirt") then
            m0 = 0.6
        elseif (v21 == "earth") then
            m0 = 0.6
        elseif (v21 == "grass") then
            m0 = 0.6
        elseif (v21 == "grass_paver") then
            m0 = 0.9
        elseif (v21 == "ground") then
            m0 = 0.7
        elseif (v21 == "sand") then
            m0 = 0.5
        elseif (v21 == "woodchips") then
            m0 = 0.5
        elseif (v21 == "snow") then
            m0 = 0.5
        elseif (v21 == "pebblestone") then
            m0 = 0.5
        elseif (v21 == "mud") then
            m0 = 0.4
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end
    if (tags["tracktype"] ~= nil) then
        m0 = nil
        local v22
        v22 = tags["tracktype"]

        if (v22 == "grade1") then
            m0 = 0.99
        elseif (v22 == "grade2") then
            m0 = 0.8
        elseif (v22 == "grade3") then
            m0 = 0.6
        elseif (v22 == "grade4") then
            m0 = 0.3
        elseif (v22 == "grade5") then
            m0 = 0.1
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end
    if (tags["incline"] ~= nil) then
        m0 = nil
        local v23
        v23 = tags["incline"]

        if (v23 == "up") then
            m0 = 0.75
        elseif (v23 == "down") then
            m0 = 1.25
        elseif (v23 == "0") then
            m0 = 1
        elseif (v23 == "0%") then
            m0 = 1
        elseif (v23 == "10%") then
            m0 = 0.9
        elseif (v23 == "-10%") then
            m0 = 1.1
        elseif (v23 == "20%") then
            m0 = 0.8
        elseif (v23 == "-20%") then
            m0 = 1.2
        elseif (v23 == "30%") then
            m0 = 0.7
        elseif (v23 == "-30%") then
            m0 = 1.3
        end


        if (m0 ~= nil) then
            r = r * m0
        end
    end


    return r
end

--[[
Gives 0 on private roads, 0.1 on destination-only roads, and 0.9 on permissive roads; gives 1 by default. This helps to select roads with no access retrictions on them

Unit: -1 | -3 | -500
Created by 
Uses tags: access
Used parameters: 
Number of combintations: 5
Returns values: 
]]
function clean_permission_score(tags, parameters)
    local r = nil
    local value0 = tags["access"]
    local v24
    v24 = value0

    if (v24 == "private") then
        r = -500
    elseif (v24 == "destination") then
        r = -3
    elseif (v24 == "permissive") then
        r = -1
    end

    if (r == nil) then
        r = 0
    end


    return r
end

--[[
Determines how safe a cyclist feels on a certain road, mostly based on car pressure. This is quite a subjective measure

Unit: safety
Created by 
Uses tags: access, bicycle:class, motor_vehicle, foot, bicycle, cyclestreet, towpath, designation, highway, cycleway, cycleway:left, cycleway:right
Used parameters: 
Number of combintations: 62
Returns values: 
]]
function bicycle_safety(tags, parameters)
    local r = nil
    r = 1
    local m1 = nil
    if (tags["access"] ~= nil) then
        m1 = nil
        local v25
        v25 = tags["access"]

        if (v25 == "no") then
            m1 = 1.5
        elseif (v25 == "destination") then
            m1 = 1.4
        elseif (v25 == "dismount") then
            m1 = 0.2
        elseif (v25 == "designated") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["bicycle:class"] ~= nil) then
        m1 = nil
        local v26
        v26 = tags["bicycle:class"]

        if (v26 == "-3") then
            m1 = 0.5
        elseif (v26 == "-2") then
            m1 = 0.7
        elseif (v26 == "-1") then
            m1 = 0.9
        elseif (v26 == "0") then
            m1 = 1
        elseif (v26 == "1") then
            m1 = 1.1
        elseif (v26 == "2") then
            m1 = 1.3
        elseif (v26 == "3") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["motor_vehicle"] ~= nil) then
        m1 = nil
        local v27
        v27 = tags["motor_vehicle"]

        if (v27 == "no") then
            m1 = 1.5
        elseif (v27 == "destination") then
            m1 = 1.4
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["foot"] ~= nil) then
        m1 = nil
        local v28
        v28 = tags["foot"]

        if (v28 == "designated") then
            m1 = 0.95
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["bicycle"] ~= nil) then
        m1 = nil
        local v29
        v29 = tags["bicycle"]

        if (v29 == "designated") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["cyclestreet"] ~= nil) then
        m1 = nil
        local v30
        v30 = tags["cyclestreet"]

        if (v30 == "yes") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["towpath"] ~= nil) then
        m1 = nil
        local v31
        v31 = tags["towpath"]

        if (v31 == "yes") then
            m1 = 1.1
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["designation"] ~= nil) then
        m1 = nil
        local v32
        v32 = tags["designation"]

        if (v32 == "towpath") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["highway"] ~= nil) then
        m1 = nil
        local v33
        v33 = tags["highway"]

        if (v33 == "cycleway") then
            m1 = 1
        elseif (v33 == "trunk") then
            m1 = 0.05
        elseif (v33 == "trunk_link") then
            m1 = 0.05
        elseif (v33 == "primary") then
            m1 = 0.1
        elseif (v33 == "secondary") then
            m1 = 0.4
        elseif (v33 == "tertiary") then
            m1 = 0.5
        elseif (v33 == "unclassified") then
            m1 = 0.8
        elseif (v33 == "track") then
            m1 = 0.95
        elseif (v33 == "residential") then
            m1 = 0.9
        elseif (v33 == "living_street") then
            m1 = 0.95
        elseif (v33 == "footway") then
            m1 = 0.95
        elseif (v33 == "path") then
            m1 = 0.9
        elseif (v33 == "construction") then
            m1 = 0.6
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["cycleway"] ~= nil) then
        m1 = nil
        local v34
        v34 = tags["cycleway"]

        if (v34 == "yes") then
            m1 = 1.15
        elseif (v34 == "lane") then
            m1 = 1.15
        elseif (v34 == "shared") then
            m1 = 1.03
        elseif (v34 == "shared_lane") then
            m1 = 1.03
        elseif (v34 == "share_busway") then
            m1 = 1.05
        elseif (v34 == "track") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["cycleway:left"] ~= nil) then
        m1 = nil
        local v35
        v35 = tags["cycleway:left"]

        if (v35 == "yes") then
            m1 = 1.15
        elseif (v35 == "lane") then
            m1 = 1.15
        elseif (v35 == "shared") then
            m1 = 1.03
        elseif (v35 == "shared_lane") then
            m1 = 1.03
        elseif (v35 == "share_busway") then
            m1 = 1.05
        elseif (v35 == "track") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end
    if (tags["cycleway:right"] ~= nil) then
        m1 = nil
        local v36
        v36 = tags["cycleway:right"]

        if (v36 == "yes") then
            m1 = 1.15
        elseif (v36 == "lane") then
            m1 = 1.15
        elseif (v36 == "shared") then
            m1 = 1.03
        elseif (v36 == "shared_lane") then
            m1 = 1.03
        elseif (v36 == "share_busway") then
            m1 = 1.05
        elseif (v36 == "track") then
            m1 = 1.5
        end


        if (m1 ~= nil) then
            r = r * m1
        end
    end


    if (r == nil) then
        r = 1
    end


    return r
end

--[[
must_match checks that a collection of tags matches a specification.

The function is not trivial and contains a few subtilities.

Consider the following source:

{"$mustMatch":{ "a":"X", "b":{"not":"Y"}}}

This is desugared into

{"$mustMatch":{ "a":{"$eq":"X"}, "b":{"not":"Y"}}}

When applied on the tags {"a" : "X"}, this yields the table {"a":"yes", "b":"yes} (as `notEq` "Y" "nil") yields "yes"..
MustMatch checks that every key in this last table yields yes - even if it is not in the original tags!


Arguments:
- The tags of the feature
- The result table, where 'attributes_to_keep' might be set
- needed_keys which indicate which keys must be present in 'tags'
- table which is the table to match

]]
function must_match(needed_keys, table, tags, result)
    for _, key in ipairs(needed_keys) do
        local v = tags[key]
        local mapping = table[key]

        if (v == nil) then
            -- a key is missing...
            -- this probably means that we must return false... unless the mapping returns something for null!
            -- note that the mapping might already be executed
            if (mapping == true or mapping == "yes") then
                -- The function for this key returned "true" despite being fed 'nil'
                -- So, we can safely assume that the absence of this key is fine!
                -- PASS
            elseif (type(mapping) == "table") then
                -- there is a mapping! We might be in luck... What does it have for 'nil'?
                local resultValue = mapping[v]
                if (resultValue == nil or resultValue == false) then
                    -- nope, no luck after all
                    return false
                end
            else
                return false
            end
        end

        if (mapping == nil) then
            -- the mapping is nil! That is fine, the key is present anyway
            -- we ignore
        elseif (type(mapping) == "table") then
            -- we have to map the value with a function:
            local resultValue = mapping[v]
            if (resultValue ~= nil or -- actually, having nil for a mapping is fine for this function!.
                    resultValue == false or
                    resultValue == "no" or
                    resultValue == "false") then
                return false
            end
        elseif (type(mapping) == "string") then
            local bool = mapping
            if (bool == "no" or bool == "0") then
                return false
            end

            if (bool ~= "yes" and bool ~= "1") then
                error("MustMatch got a string value it can't handle: " .. bool)
            end
        elseif (type(mapping) == "boolean") then
            if (not mapping) then
                return false
            end
        else
            error("The mapping is not a table. This is not supported. We got " .. tostring(mapping) .. " (" .. type(mapping) .. ")")
        end
    end

    -- Now that we know for sure that every key matches, we add them all to the 'attributes_to_keep'
    if (result == nil) then
        -- euhm, well, seems like we don't care about the attributes_to_keep; early return!
        return true
    end
    for _, key in ipairs(needed_keys) do
        local v = tags[key] -- this is the only place where we use the original tags
        if (v ~= nil) then
            result.attributes_to_keep[key] = v
        end
    end

    return true
end

function table_to_list(tags, result, factor_table)
    local list = {}
    if(tags == nil) then
        return list
    end
    for key, mapping in pairs(factor_table) do
        local v = tags[key]
        if (v ~= nil) then
            if (type(mapping) == "table") then
                local f = mapping[v]
                if (f ~= nil) then
                    table.insert(list, f);
                    if (result.attributes_to_keep ~= nil) then
                        result.attributes_to_keep[key] = v
                    end
                end
            else
                table.insert(list, mapping);
                if (result.attributes_to_keep ~= nil) then
                    result.attributes_to_keep[key] = v
                end
            end
        end
    end

    return list;
end

function stringToTags(table, tags)
    if (tags == nil) then
        return table
    end
    return  table_to_list(tags, {}, table)
end

function eq(a, b)
    if (a == b) then
        return "yes"
    else
        return "no"
    end
end


function head(ls)
    if(ls == nil) then
        return nil
    end
    for _, v in pairs(ls) do
        if(v ~= nil) then
            return v
        end
    end
    return nil
end

function parse(string)
    if (string == nil) then
        return nil
    end
    if (type(string) == "number") then
        return string
    end

    if (string == "yes" or string == "true") then
        return 1
    end

    if (string == "no" or string == "false") then
        return 0
    end

    if (type(string) == "boolean") then
        if (string) then
            return 1
        else
            return 0
        end
    end

    if(string:match("%d+:%d+")) then
        -- duration in minute
        local duration = 0
        for part in string:gmatch "%d+" do
            duration = duration * 60 + tonumber(part)
        end
        return duration
    end


    return tonumber(string)
end

function multiply(list)
    local factor = 1
    for _, value in pairs(list) do
        if (value ~= nil) then
            factor = factor * value
        end
    end
    return factor;
end

function if_then_else(condition, thn, els)
    if (condition ~= nil and (condition == "yes" or condition == true or condition == "true")) then
        return thn
    else
        return els -- if no third parameter is given, 'els' will be nil
    end
end

function is_null(a)
    return a == nil;
end

function inv(n)
    return 1/n
end

function sum(list)
    local sum = 0
    for _, value in pairs(list) do
        if(value ~= nil) then
            if(value == 'yes' or value == 'true') then
                value = 1
            end
            sum = sum + value
        end
    end
    return sum;
end

function string_start(strt, s)
    return string.sub(s, 1, string.len(strt)) == strt
end


-- every key starting with "_relation:<name>:XXX" is rewritten to "_relation:XXX"
function remove_relation_prefix(tags, name)

    local new_tags = {}
    for k, v in pairs(tags) do
        local prefix = "_relation:" .. name .. ":";
        if (string_start(prefix, k)) then
            local new_key = "_relation:" .. string.sub(k, string.len(prefix) + 1) -- plus 1: sub uses one-based indexing to select the start
            new_tags[new_key] = v
        else
            new_tags[k] = v
        end
    end
    return new_tags
end

function debug_table(table, prefix)
    if (prefix == nil) then
        prefix = ""
    end
    for k, v in pairs(table) do

        if (type(v) == "table") then
            debug_table(v, "   ")
        else
            print(prefix .. tostring(k) .. " = " .. tostring(v))
        end
    end
    print("")
end

function debug_table_str(table, prefix)
    if (prefix == nil) then
        prefix = ""
    end
    local str = "";
    for k, v in pairs(table) do

        if (type(v) == "table") then
            str = str .. "," .. debug_table_str(v, "   ")
        else
            str = str .. "," .. (prefix .. tostring(k) .. " = " .. tostring(v))
        end
    end
    return str
end

function max(list)
    local max
    for _, value in pairs(list) do
        if (value ~= nil) then
            if (max == nil) then
                max = value
            elseif (max < value) then
                max = value
            end
        end
    end

    return max;
end

function min(list)
    local min
    for _, value in pairs(list) do
        if(value ~= nil) then
            if (min == nil) then
                min = value
            elseif (value < min) then
                min = value
            end
        end
    end

    return min;
end

function atleast(minimumExpected, actual, thn, els)
    if (minimumExpected <= actual) then
        return thn;
    end
    return els
end

function containedIn(list, a)
    for _, value in pairs(list) do
        if (value == a) then
            return true
        end
    end

    return false;
end

--[[
 Splits a string on the specified character, e.g.
 str_split("abc;def;ghi", ";") will result in a table ["abc","def","ghi"]
]]
function str_split (inputstr, sep)
    if sep == nil then
        sep = "%s"
    end
    local t={}
    for str in string.gmatch(inputstr, "([^"..sep.."]+)") do
        table.insert(t, str)
    end
    return t
end


--[[ 

  
 Returns '0' if the turn is allowed and '-1' if the turn is forbidden.
 Only used by itinero 2.0.
 
 The itinero 2.0 profile outputs a `turn_cost_factor` which immediately calls this one (see LuaPrinter2.MainFunction).
 
 Dependencies: str_split, containedIn
]]
function calculate_turn_cost_factor(attributes, vehicle_types)

    if (attributes["type"] ~= "restriction") then
        -- not a turn restriction; no cost to turn,
        return 0
    end

    for _, vehicle_type in pairs(vehicle_types) do
        if (attributes["restriction:"..vehicle_type] ~= nil) then
            -- There is a turn restriction specifically for one of our vehicle types!
            return -1
        end
    end

    if (attributes["restriction"] == nil) then
        -- Not a turn restriction; no cost to turn
        return 0
    end
    if (attributes["except"] ~= nil) then
        local except_types = str_split(attributes["except"],";")
        for _, vehicle_type in pairs(vehicle_types) do
            if (containedIn(except_types, vehicle_type)) then
                -- This vehicle is exempt from this turn restriction
                return 0
            end
        end
    end

    return -1
end






