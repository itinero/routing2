name = "pedestrian"
vehicle_types = { "foot" }

speed_profile = {
    ["primary"] = { speed = 5, access = true },
    ["primary_link"] = { speed = 5, access = true },
    ["secondary"] = { speed = 5, access = true },
    ["secondary_link"] = { speed = 5, access = true },
    ["tertiary"] = { speed = 5, access = true },
    ["tertiary_link"] = { speed = 5, access = true },
    ["unclassified"] = { speed = 5, access = true },
    ["residential"] = { speed = 5, access = true },
    ["service"] = { speed = 5, access = true },
    ["services"] = { speed = 5, access = true },
    ["road"] = { speed = 5, access = true },
    ["track"] = { speed = 5, access = true },
    ["cycleway"] = { speed = 5, access = true },
    ["footway"] = { speed = 5, access = true },
    ["pedestrian"] = { speed = 5, access = true },
    ["path"] = { speed = 5, access = true },
    ["living_street"] = { speed = 5, access = true },
    ["ferry"] = { speed = 5, access = true },
    ["movable"] = { speed = 5, access = true },
    ["shuttle_train"] = { speed = 5, access = true },
    ["default"] = { speed = 5, access = true }
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
        result.forward = 1 / highway_speed.speed / 3.6
        result.backward = result.forward
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
    local direction = is_oneway(attributes, "oneway:foot")
    if direction != nil then
        result.direction = direction
    end
        
    if result.direction == 1 then
        result.backward = 0
    elseif result.direction == 2 then
        result.forward = 0
    end    
end