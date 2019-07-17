name = "bicycle"
vehicle_types = { "vehicle", "bicycle" }

speed_profile = {
    ["primary"] = { speed = 15, access = true },
    ["primary_link"] = { speed = 15, access = true },
    ["secondary"] = { speed = 15, access = true },
    ["secondary_link"] = { speed = 15, access = true },
    ["tertiary"] = { speed = 15, access = true },
    ["tertiary_link"] = { speed = 15, access = true },
    ["unclassified"] = { speed = 15, access = true },
    ["residential"] = { speed = 15, access = true },
    ["service"] = { speed = 15, access = true },
    ["services"] = { speed = 15, access = true },
    ["road"] = { speed = 15, access = true },
    ["track"] = { speed = 15, access = true },
    ["cycleway"] = { speed = 15, access = true },
    ["footway"] = { speed = 15, access = false },
    ["pedestrian"] = { speed = 15, access = false },
    ["path"] = { speed = 15, access = true },
    ["living_street"] = { speed = 15, access = true },
    ["ferry"] = { speed = 15, access = true },
    ["movable"] = { speed = 15, access = true },
    ["shuttle_train"] = { speed = 15, access = true },
    ["default"] = { speed = 15, access = true }
}

access_values = {
    ["private"] = false,
    ["yes"] = true,
    ["no"] = false,
    ["permissive"] = true,
    ["destination"] = true,
    ["customers"] = false,
    ["designated"] = true,
    ["public"] = true,
    ["delivery"] = true,
    ["use_sidepath"] = false
}

function can_access(attributes, result)
    local last_access
    local access = access_values[attributes.access]
    if access then
        last_access = access
    end
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        local access_key = attributes[access_key_key]
        if access_key then
            access = access_values[access_key]
            if access then
                last_access = access
            end
        end
    end
    return last_access
end

-- turns a oneway tag value into a direction
function is_oneway(attributes, name)
    local oneway = attributes[name]
    if oneway == "yes" or oneway == "true" or oneway == "1" then
        return 1
    end
    if oneway == "-1" then
        return 2
    end
    if oneway == "no" then
        return 0
    end
    return nil
end

-- based on: https://wiki.openstreetmap.org/wiki/Key:surface
best_surface = 1
good_surface = 0.9
bad_surface = 0.7
no_surface = 0.5

surface_factors = {
   asphalt = best_surface,
   ["cobblestone:flattened"] = best_surface,
   paving_stones = best_surface,
   compacted = good_surface,
   cobblestone = bad_surface,
   unpaved = bad_surface,
   fine_gravel = bad_surface,
   gravel = bad_surface,
   pebblestone = bad_surface,
   ground = bad_surface,
   dirt = bad_surface,
   earth = bad_surface,
   grass = no_surface,
   mud = no_surface,
   sand = no_surface,
   sett = good_surface
}

function surface_factor(attributes, result)
    local factor = surface_factors[attributes.surface]
    if factor then
        return factor
    end
    return 1 -- the default factor, assume paved by default.
end

function factor(attributes, result)
    result.forward = 0
    result.backward = 0

    if not attributes then
        return
    end

    local highway = attributes.highway

    -- set highway to ferry when ferry.
    local route = attributes.route;
    if route == "ferry" then
        highway = "ferry"
    end

    -- get speed and access per highway type.
    local highway_speed = speed_profile[highway]
    if highway_speed then
        result.forward = 1 / (highway_speed.speed / 3.6)
        result.backward = result.forward
        result.access = highway_speed.access
    else
        return
    end
    result.forward_speed = 3.6 / result.forward
    result.backward_speed = 3.6 / result.backward

    result.access = can_access(attributes, result)    
    if not result.access == nil then
        result.forward = 0
        result.backward = 0
        return
    end

    -- get directional information 
    -- reset forward/backward factors
    local junction = attributes.junction
    if junction == "roundabout" then
        result.direction = 1
    end
    local direction = is_oneway(attributes, "oneway")
    if direction != nil then
        result.direction = direction
    end
    local direction = is_oneway(attributes, "oneway:bicycle")
    if direction != nil then
        result.direction = direction
    end
        
    if result.direction == 1 then
        result.backward = 0
    elseif result.direction == 2 then
        result.forward = 0
    end    
    
    itinero.log("before surface")
    itinero.log(result)
    
    -- process surface factors
    local surface = surface_factor(attributes, result)
    if surface then
        result.forward = result.forward / surface
        result.backward = result.backward / surface
    end

    itinero.log("after surface")
    itinero.log(result)
end