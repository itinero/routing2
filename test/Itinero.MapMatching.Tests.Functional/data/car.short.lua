name = "car.short"
description = "The shortest route, independent of speed (Profile for a normal car)"

-- The hierarchy of types that this vehicle is; mostly used to check access restrictions
vehicle_types = {"vehicle", "motor_vehicle", "motorcar", "car"}

--[[
Calculate the actual factor.forward and factor.backward for a segment with the given properties
]]
function factor(tags, result)

    -- Cleanup the relation tags to make them usable with this profile
    tags = remove_relation_prefix(tags, "short")

    -- initialize the result table on the default values
    result.forward_speed = 0
    result.backward_speed = 0
    result.forward = 0
    result.backward = 0
    result.canstop = true
    result.attributes_to_keep = {} -- not actually used anymore, but the code generation still uses this


    local parameters = default_parameters()
    parameters.distance = 1


    local oneway = if_then_else(parameters["follow_restrictions"], car_oneway(tags), "both")
    tags.oneway = oneway
    -- An aspect describing oneway should give either 'both', 'against' or 'width'


    -- forward calculation. We set the meta tag '_direction' to 'width' to indicate that we are going forward. The other functions will pick this up
    tags["_direction"] = "with"
    local access_forward = head({legal_access_be(tags), car_legal_access(tags)})
    if(oneway == "against") then
        -- no 'oneway=both' or 'oneway=with', so we can only go back over this segment
        -- we overwrite the 'access_forward'-value with no; whatever it was...
        access_forward = "no"
    end
    if(access_forward ~= nil and access_forward ~= "no" and access_forward ~= false) then
        tags.access = access_forward -- might be relevant, e.g. for 'access=dismount' for bicycles
        result.forward_speed = min({legal_maxspeed_be(tags), car_practical_max_speed(tags), parameters["maxspeed"]})
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
    local access_backward = head({legal_access_be(tags), car_legal_access(tags)})
    if(oneway == "with") then
        -- no 'oneway=both' or 'oneway=against', so we can only go forward over this segment
        -- we overwrite the 'access_forward'-value with no; whatever it was...
        access_backward = "no"
    end
    if(access_backward ~= nil and access_backward ~= "no" and access_backward ~= false) then
        tags.access = access_backward
        result.backward_speed = min({legal_maxspeed_be(tags), car_practical_max_speed(tags), parameters["maxspeed"]})
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
    1 * distance

    local scalingfactor
    local value = tags["access"]
    local v13
    v13 = value

    if (v13 == "destination") then
        scalingfactor = if_then_else(parameters["follow_restrictions"], 0.01)
    elseif (v13 == "customers") then
        scalingfactor = 0.01
    elseif (v13 == "private") then
        scalingfactor = 0.0001
    end

    if (scalingfactor == nil) then
        scalingfactor = 1
    end


    return priority * scalingfactor
end

--[[ Function called by itinero2 on every turn restriction relation
 ]]
function turn_cost_factor(attributes, result)
    local parameters = default_parameters()
    parameters.distance = 1

    local has_access
    local cond1
    cond1 = parameters["follow_restrictions"]

    if ( cond1 == true or cond1 == "yes" ) then
        local value0 = attributes["barrier"]
        local v14
        v14 = value0

        if (v14 == "bollard") then
            has_access = "no"
        elseif (v14 == "sump_buster") then
            has_access = "no"
        end
    end


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
    parameters.maxspeed = 120
    parameters.timeNeeded = 0
    parameters.distance = 0
    parameters.classification_importance = 0
    parameters.follow_restrictions = "yes"

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
Gives, for each type of highway, whether or not a car can enter legally.
Note that legal access is a bit 'grey' in the case of roads marked private and permissive, in which case these values are returned

Unit: 'yes': access allowed, 'permissive': bicycles are allowed here, but this might be a private road or service where usage is allowed, but could be retracted one day by the owner
'dismount': cycling here is not allowed, but walking with the bicycle is
'destination': driving is allowed here, but only if truly necessary to reach the destination
'private': this is a private road, only go here if the destination is here
'no': do not cycle here
Created by 
Uses tags: area, access, highway, service, motor_vehicle, anyways:motor_vehicle, anyways:access, anyways:construction
Used parameters: 
Number of combintations: 50
Returns values: 
]]
function car_legal_access(tags, parameters)
    local r = nil
    if (tags["highway"] ~= nil) then
        local v
        v = tags["highway"]

        if (v == "residential") then
            r = "yes"
        elseif (v == "living_street") then
            r = "yes"
        elseif (v == "service") then
            r = "destination"
        elseif (v == "services") then
            r = "destination"
        elseif (v == "track") then
            r = "yes"
        elseif (v == "motorway") then
            r = "yes"
        elseif (v == "motorway_link") then
            r = "yes"
        elseif (v == "trunk") then
            r = "yes"
        elseif (v == "trunk_link") then
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
        end
    end
    if (tags["service"] ~= nil) then
        local v0
        v0 = tags["service"]

        if (v0 == "parking_aisle") then
            r = "destination"
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
            r = "destination"
        elseif (v1 == "private") then
            r = "private"
        elseif (v1 == "permissive") then
            r = "permissive"
        elseif (v1 == "destination") then
            r = "destination"
        elseif (v1 == "delivery") then
            r = "destination"
        elseif (v1 == "service") then
            r = "destination"
        elseif (v1 == "permit") then
            r = "destination"
        end
    end
    if (tags["motor_vehicle"] ~= nil) then
        local v2
        v2 = tags["motor_vehicle"]

        if (v2 == "yes") then
            r = "yes"
        elseif (v2 == "no") then
            r = "no"
        elseif (v2 == "permissive") then
            r = "destination"
        elseif (v2 == "private") then
            r = "private"
        elseif (v2 == "permit") then
            r = "destination"
        elseif (v2 == "destination") then
            r = "destination"
        elseif (v2 == "customers") then
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
    if (tags["anyways:motor_vehicle"] ~= nil) then
        r = tags["anyways:motor_vehicle"]

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
Determines wether or not a car can go in both ways in this street, and if it is oneway, in what direction

Unit: both: direction is allowed in both direction
with: this is a oneway street with direction allowed with the grain of the way
against: oneway street with direction against the way
Created by 
Uses tags: anyways:oneway, oneway, junction
Used parameters: 
Number of combintations: 13
Returns values: 
]]
function car_oneway(tags, parameters)
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
    if (tags["junction"] ~= nil) then
        local v7
        v7 = tags["junction"]

        if (v7 == "roundabout") then
            r = "with"
        end
    end
    if (tags["anyways:oneway"] ~= nil) then
        local v8
        v8 = tags["anyways:oneway"]

        if (v8 == "yes") then
            r = "with"
        elseif (v8 == "no") then
            r = "both"
        elseif (v8 == "1") then
            r = "with"
        elseif (v8 == "-1") then
            r = "against"
        end
    end


    if (r == nil) then
        r = "both"
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
        local v9
        v9 = tags["highway"]

        if (v9 == "cycleway") then
            r = 30
        elseif (v9 == "footway") then
            r = 20
        elseif (v9 == "crossing") then
            r = 20
        elseif (v9 == "pedestrian") then
            r = 15
        elseif (v9 == "path") then
            r = 15
        elseif (v9 == "corridor") then
            r = 5
        elseif (v9 == "residential") then
            r = 30
        elseif (v9 == "living_street") then
            r = 20
        elseif (v9 == "service") then
            r = 30
        elseif (v9 == "services") then
            r = 30
        elseif (v9 == "track") then
            r = 20
        elseif (v9 == "unclassified") then
            r = 50
        elseif (v9 == "road") then
            r = 50
        elseif (v9 == "motorway") then
            r = 120
        elseif (v9 == "motorway_link") then
            r = 120
        elseif (v9 == "trunk") then
            r = 90
        elseif (v9 == "trunk_link") then
            r = 90
        elseif (v9 == "primary") then
            r = 90
        elseif (v9 == "primary_link") then
            r = 90
        elseif (v9 == "secondary") then
            r = 70
        elseif (v9 == "secondary_link") then
            r = 70
        elseif (v9 == "tertiary") then
            r = 50
        elseif (v9 == "tertiary_link") then
            r = 50
        elseif (v9 == "construction") then
            r = 10
        end
    end
    if (tags["designation"] ~= nil) then
        local v10
        v10 = tags["designation"]

        if (v10 == "towpath") then
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
In some cases, the actual speed of a car will be constrained by more factors then just the legal max speed - e.g. in a cyclestreet where overtaking cyclists is prohibeted. Even though driving 30km/h is allowed, in practice it'll only be ~15km/h

Unit: km/h
Created by 
Uses tags: cyclestreet, highway
Used parameters: 
Number of combintations: 5
Returns values: 
]]
function car_practical_max_speed(tags, parameters)
    local r = nil
    if (tags["cyclestreet"] ~= nil) then
        local v11
        v11 = tags["cyclestreet"]

        if (v11 == "yes") then
            r = 15
        end
    end
    if (tags["highway"] ~= nil) then
        local v12
        v12 = tags["highway"]

        if (v12 == "track") then
            r = 5
        end
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

function if_then_else(condition, thn, els)
    if (condition ~= nil and (condition == "yes" or condition == true or condition == "true")) then
        return thn
    else
        return els -- if no third parameter is given, 'els' will be nil
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






