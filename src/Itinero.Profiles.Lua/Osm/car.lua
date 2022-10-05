name = "car"
vehicle_types = { "vehicle", "motorvehicle", "motorcar" }

speed_profile = {
    ["motorway"] = { speed = 120, access = true },
    ["motorway_link"] = { speed = 120, access = true },
    ["trunk"] = { speed = 120, access = true },
    ["trunk_link"] = { speed = 120, access = true },
    ["primary"] = { speed = 90, access = true },
    ["primary_link"] = { speed = 90, access = true },
    ["secondary"] = { speed = 70, access = true },
    ["secondary_link"] = { speed = 70, access = true },
    ["tertiary"] = { speed = 70, access = true },
    ["tertiary_link"] = { speed = 70, access = true },
    ["unclassified"] = { speed = 50, access = true },
    ["residential"] = { speed = 50, access = true },
    ["service"] = { speed = 50, access = true },
    ["services"] = { speed = 50, access = true },
    ["road"] = { speed = 15, access = true },
    ["track"] = { speed = 15, access = false },
    ["cycleway"] = { speed = 15, access = false },
    ["footway"] = { speed = 15, access = false },
    ["pedestrian"] = { speed = 15, access = false },
    ["path"] = { speed = 15, access = false },
    ["living_street"] = { speed = 15, access = true },
    ["ferry"] = { speed = 15, access = true },
    ["movable"] = { speed = 15, access = false },
    ["shuttle_train"] = { speed = 15, access = false },
    ["default"] = { speed = 15, access = false }
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

function factor(attributes, result)
    result.forward = 0
    result.backward = 0
    result.forward_speed = 0
    result.backward_speed = 0

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
        result.forward = 1 / (highway_speed.speed / 36)
        result.forward_speed = highway_speed.speed
        result.backward = result.forward
        result.backward_speed = highway_speed.speed
        result.access = highway_speed.access
    else
        return
    end

    local access = can_access(attributes, result)
    if not access == nil then
        result.access = access
    end

    if result.access then
    else
        result.forward = 0
        result.backward = 0
        result.forward_speed = 0
        result.backward_speed = 0
        return
    end

    -- get directional information 
    -- reset forward/backward factors
    local junction = attributes.junction
    if junction == "roundabout" then
        result.direction = 1
    end
    local direction = is_oneway(attributes, "oneway")
    if direction ~= nil then
        result.direction = direction
    end
        
    if result.direction == 1 then
        result.backward = 0
        result.backward_speed = 0
    elseif result.direction == 2 then
        result.forward = 0
        result.forward_speed = 0
    end    
end

function turn_cost_factor(attributes, result)
    result.factor = 0
    
    -- get factors for barriers, if any.
    local barrier = attributes.barrier
    if barrier ~= nil then
        result.factor = -1
        return
    end

    -- get factors for turn restrictions.
    local type = attributes.type
    if type ~= "restriction" then
        return
    end
    
    result.factor = -1
end