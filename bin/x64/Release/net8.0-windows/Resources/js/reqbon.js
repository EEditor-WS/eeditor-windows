// Конфигурация полей для требований и бонусов
const reqbonConfig = {
    // Конфигурация бонусов
    bonuses: {
        // Бонусы с длительностью
        defense: { hasDuration: true, defaultDuration: 3 },
        attack: { hasDuration: true, defaultDuration: 3 },
        population_income: { hasDuration: true, defaultDuration: 3 },
        population_increase: { hasDuration: true, defaultDuration: 3 },
        population_increase: { hasDuration: true, defaultDuration: 3 },
        building_cost: { hasDuration: true, defaultDuration: 3 },
        relation_ideology_change: { hasDuration: true },
        relation_change: { hasDuration: true },
        accelerated_recruit_cost: { hasDuration: true },
        maintaining_army_cost_multiplier: { hasDuration: true },
        population_increase: { hasDuration: true },
        recruit_cost: { hasDuration: true },
        add_culture_population: { hasDuration: true },
        discontent: { hasDuration: true },

        // Бонусы без длительности
        add_resource: { 
            hasDuration: false,
            subtypes: [
                'gold',
                'iron',
                'oil',
                'steel',
                'uranium',
                'wood',
                'cartridges',
                'chemical_weapon',
                'heavy_water',
                'nuclear_weapon'
            ]
        },
        resurrect_country: { hasDuration: false },
        add_oil: { hasDuration: false },
        add_cruiser: { hasDuration: false },
        add_artillery: { hasDuration: false },
        diplomacy_lift_sanctions: { hasDuration: false },
        diplomacy_sanctions: { hasDuration: false },
        diplomacy_pact: { hasDuration: false },
        add_random_culture_population: { hasDuration: false },
        diplomacy_alliance: { hasDuration: false },
        diplomacy_peace: { hasDuration: false },
        diplomacy_become_vassal: { hasDuration: false },
        diplomacy_get_vassal: { hasDuration: false },
        resource: { hasDuration: false },
        add_shock_infantry: { hasDuration: false },
        annex_country: { hasDuration: false },
        add_tank: { hasDuration: false },
        diplomacy_war: { hasDuration: false },
        army_losses: { hasDuration: false },
        prestige: { hasDuration: false },
        add_battleship: { hasDuration: false },
        add_infantry: { hasDuration: false },
        science: { hasDuration: false },
        money: { hasDuration: false },
        change_country: { hasDuration: false },
        change_political_institution: { hasDuration: false },
        disable_external_diplomacy: { hasDuration: true },
    },

    // Конфигурация требований
    requirements: {
        building_exists: {
            subType: false,
            value: 'building',
            actions: ['equal', 'not_equal']
        },
        controls_capital: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        cooldown: {
            subType: 'event',
            value: 'number',
            actions: ['more', 'less']
        },
        count_of_tasks: {
            subType: false,
            value: 'number',
            actions: ['more', "equal", 'less']
        },
        discontent: {
            subType: false,
            value: 'number',
            actions: ['more', 'less']
        },
        enemy_near_capital: {
            subType: false,
            value: 'boolean',
            actions: ['equal', 'not_equal']
        },
        event_choice: {
            subType: 'event',
            value: 'number',
            actions: 'country'
        },
        group_name: {
            subType: false,
            value: 'text',
            actions: ['equal', 'not_equal']
        },
        has_alliance: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        has_pact: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        has_provinces: {
            subType: ['all', 'any'],
            value: 'text',
            actions: ['equal', 'not_equal']
        },
        has_sanctions: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        has_vassal: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        has_war: {
            subType: 'country',
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        independent_land: {
            subType: false,
            value: [true],
            actions: ['equal', 'not_equal']
        },
        is_defeated: {
            subType: false,
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        is_neighbor: {
            subType: false,
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        is_player: {
            subType: false,
            value: [true],
            actions: ['equal', 'not_equal']
        },
        land_id: {
            subType: false,
            value: 'country',
            actions: ['equal', 'not_equal']
        },
        land_name: {
            subType: false,
            value: 'text',
            actions: ['equal', 'not_equal']
        },
        land_power: {
            subType: false,
            value: 'number',
            actions: ['more', 'less']
        },
        lost_capital: {
            subType: false,
            value: [true],
            actions: ['equal', 'not_equal']
        },
        money: {
            subType: false,
            value: 'number',
            actions: ['more', 'less']
        },
        month: {
            subType: false,
            value: 'month',
            actions: ['more', 'equal', 'less']
        },
        near_water: {
            subType: false,
            value: [true],
            actions: ['equal', 'not_equal']
        },
        no_enemy: {
            subType: false,
            value: [true],
            actions: ['equal', 'not_equal']
        },
        num_of_provinces: {
            subType: false,
            value: 'number',
            actions: ['more', 'equal', 'less']
        },
        num_of_vassals: {
            subType: false,
            value: 'number',
            actions: ['more', 'equal', 'less']
        },
        political_institution: {
            subType: false,
            value: 'ideology',
            actions: ['equal', 'not_equal']
        },
        random_value: {
            subType: false,
            value: 'zerosto',
            actions: ['less']
        },
        received_event: {
            subType: 'country',
            value: 'event',
            actions: ['equal', 'not_equal']
        },
        tax: {
            subType: false,
            value: 'number',
            actions: ['more', 'less']
        },
        turn: {
            subType: false,
            value: 'number',
            actions: ['more', 'equal', 'less']
        },
        year: {
            subType: false,
            value: 'number',
            actions: ['more', 'equal', 'less']
        }
    }
};

// Экспортируем конфигурацию
window.reqbonConfig = reqbonConfig;

window.requirementsManager = {
    // Get requirements/bonuses from a specific section
    getRequirements(section) {
        const container = document.getElementById(`event-${section}`);
        if (!container) {
            console.warn(`Container for ${section} not found`);
            return [];
        }

        const items = [];
        const itemElements = container.getElementsByClassName('array-list-item');
        
        Array.from(itemElements).forEach(item => {
            const content = item.querySelector('.item-content')?.textContent;
            if (!content) return;

            // Parse item content to extract type, action, value and subtype
            const parsed = this.parseItemContent(content);
            if (parsed) {
                items.push(parsed);
            }
        });

        return items;
    },

    // Parse item content string into requirement/bonus object
    parseItemContent(content) {
        // Basic format: "type action value (subtype)"
        const match = content.match(/^(\w+)\s*(\w+)?\s*([^(]+)(?:\(([^)]+)\))?$/);
        if (!match) return null;

        const [_, type, action, value, subtype] = match;
        
        // Create requirement object
        const requirement = {
            type: type.trim(),
            value: value.trim()
        };

        // Add action if present
        if (action && action !== 'undefined') {
            requirement.action = action.trim();
        }

        // Add subtype if present
        if (subtype) {
            requirement.subtype = subtype.trim();
        }

        return requirement;
    }
};

// Функция для создания селекта со странами
function createCountrySelect(selectedValue = '') {
    const select = document.createElement('select');
    select.className = 'main-page-input';
    
    // Добавляем специальные опции
    select.innerHTML = `
        <option value="any" ${selectedValue === 'any' ? 'selected' : ''}>${window.translator?.translate('any') || 'any'}</option>
        <option value="this" ${selectedValue === 'this' ? 'selected' : ''}>${window.translator?.translate('this') || 'this'}</option>
    `;
    
    // Получаем список стран и сортируем по имени
    const countries = Object.entries(window.eventManager?.jsonData?.lands || {})
        .map(([id, country]) => ({
            id,
            name: country.name || id
        }))
        .sort((a, b) => a.name.toString().toUpperCase().localeCompare(b.name.toString().toUpperCase()));
    
    // Добавляем страны в select
    select.innerHTML += countries.map(country => 
        `<option value="${country.id}" ${selectedValue === country.id ? 'selected' : ''}>${country.name}</option>`
    ).join('');
    
    return select;
}

// Обновляем функцию создания поля значения
function updateValueField(type, currentValue = '') {
    const valueContainer = document.getElementById('requirement-value-container');
    if (!valueContainer) return;
    
    valueContainer.innerHTML = '';
    
    const config = window.reqbonConfig.requirements[type];
    if (!config) return;
    
    if (config.value === 'country') {
        const select = createCountrySelect(currentValue);
        select.id = 'requirement-value';
        valueContainer.appendChild(select);
    }
    // Остальная логика для других типов...
}

window.raccoon = {
    money: {
        title :  "Money",
        description :  "Amount of money in treasury",
        type :  "number",
        actions :  ["more", "less"]
    },
    political_institution :  {
        title :  "Political Institution",
        description :  "Specific political institution is opened",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    count_of_tasks :  {
        title :  "Number of Tasks",
        description :  "Number of active tasks in top right corner",
        type :  "number",
        actions :  ["more", "equal", "less"]
    },
    enemy_near_capital :  {
        title :  "Enemy Near Capital",
        description :  "Enemy forces are near the capital",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    lost_capital :  {
        title :  "Lost Capital",
        description :  "The capital has been lost in battle",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    discontent :  {
        title :  "Discontent",
        description :  "Level of population discontent",
        type :  "number",
        actions :  ["more", "less"]
    },
    tax :  {
        title :  "Tax Rate",
        description :  "The current tax rate",
        type :  "number",
        actions :  ["more", "less"]
    },
    land_name :  {
        title :  "Country Name",
        description :  "Specific land by its name (will not work if country changed it's name or will be reformed)",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    land_id :  {
        title :  "Country",
        description :  "Specific country, regardless of how its name changes during the game",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    group_name :  {
        title :  "Country Group",
        description :  "Group of countries for event targeting",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    random_value :  {
        title :  "Random Value",
        description :  "Compare with a random value between 0 and 1 (useful for chances: less 0.2 :  20% chance)",
        type :  "number",
        actions :  ["less"]
    },
    no_enemy :  {
        title :  "No Enemy",
        description :  "There are no enemy country that can reach player's territory",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    building_exists :  {
        title :  "Building Exists",
        description :  "Specific building is placed on one of the player's provinces",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    num_of_provinces :  {
        title :  "Number of Provinces",
        description :  "Total number of controlled provinces",
        type :  "number",
        actions :  ["equal", "more", "less"]
    },
    near_water :  {
        title :  "Near Water",
        description :  "There is water province near the territory",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    independent_land :  {
        title :  "Independent Land",
        description :  "The land is not a vassal",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    land_power :  {
        title :  "Land Power",
        description :  "Military power of a land",
        type :  "number",
        actions :  ["more", "less"]
    },
    turn :  {
        title :  "Turn",
        description :  "Current game turn number",
        type :  "number",
        actions :  ["more", "equal", "less"]
    },
    year :  {
        title :  "Year",
        description :  "Current game year",
        type :  "number",
        actions :  ["more", "equal", "less"]
    },
    month :  {
        title :  "Month",
        description :  "Current game month",
        type :  "number",
        actions :  ["more", "equal", "less"]
    },
    cooldown :  {
        title :  "No more than once every",
        description :  "Check time passed since last event occurrence (if event didn't happen, more will always be true, less will always be false)",
        type :  "number",
        actions :  ["more", "less"]
    },
    received_event :  {
        title :  "Received Event",
        description :  "Check if country received specific event before (or any country received it)",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    event_choice :  {
        title :  "Event Choice",
        description :  "Check which choice was selected in a specific event by a country",
        type :  "string",
        subtypes :  `function(gdt)
            local events :  {}
            local event_names :  {}
            
            for id, event in pairs(gdt.custom_events) do
                table.insert(events, id)
                table.insert(event_names, event.title)
            end
            
            return events, event_names
        end`
    },
    has_war :  {
        title :  "Has War",
        description :  "Check if two countries are at war or if country has any wars",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    has_sanctions :  {
        title :  "Has Sanctions",
        description :  "Check if sanctions are imposed between countries or if country has any sanctions",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    has_pact :  {
        title :  "Has Pact",
        description :  "Check if two countries have a pact or if country has any pacts",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    has_alliance :  {
        title :  "Has Alliance",
        description :  "Check if two countries have an alliance or if country has any alliances",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    is_defeated :  {
        title :  "Is Defeated",
        description :  "Check if a country has been defeated",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    controls_capital :  {
        title :  "Controls Capital",
        description :  "Check if a country controls another country's capital",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    is_player :  {
        title :  "Is Player",
        description :  "Check if a country is controlled by a player",
        type :  "boolean",
        actions :  ["equal", "not_equal"]
    },
    has_vassal :  {
        title :  "Has Vassal",
        description :  "Check if a country has a specific vassal or any vassal",
        type :  "string",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            local countries :  {"any", "this"}
            local country_names :  {"Any", "This country"}
            
            for key, value in pairs(gdt.lands) do
                if key ~= "undeveloped_land" then
                    table.insert(countries, key)
                    table.insert(country_names, value.name)
                end
            end
            
            return countries, country_names
        end`
    },
    is_neighbor :  {
        title :  "Is Neighbor",
        description :  "Check if a country borders another country",
        type :  "string",
        actions :  ["equal", "not_equal"]
    },
    num_of_vassals :  {
        title :  "Number of Vassals",
        description :  "Total number of vassals under this country",
        type :  "number",
        actions :  ["equal", "more", "less"]
    },
    has_provinces :  {
        title :  "Has Provinces",
        description :  "Check if country controls specific provinces",
        type :  "provinces",
        actions :  ["equal", "not_equal"],
        subtypes :  `function(gdt)
            return {"all", "any"}, {"All provinces", "Any province"}
        end`
    },
};

// 1. Получаем массив записей [ключ, значение]
const entries = Object.entries(window.raccoon);

// 2. Сортируем массив по алфавиту, обращаясь к полю title
entries.sort((a, b) => a[0].localeCompare(b[0]));

// 3. Создаем новый объект из отсортированного массива
window.raccoon = Object.fromEntries(entries);

console.log(window.raccoon);