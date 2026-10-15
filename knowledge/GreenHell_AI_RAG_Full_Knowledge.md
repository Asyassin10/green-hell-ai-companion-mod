# Green Hell AI Companion — Searchable Gameplay Knowledge

> RAG ingestion edition | Language: English | Scope: Green Hell PC, Story / Survival / Spirits of Amazonia | Compiled 2026-10-15
>
> This is an **original, structured synthesis**, not a verbatim copy of a game wiki. It is a substantial starter knowledge corpus, **not an exhaustive database of every item, quest, or map marker**. Some content and coordinates vary by game mode/version. Keep records separated by headings when chunking. The assistant must not invent missing details. Story spoilers are explicitly marked.

## 00 — How the AI should use this document

- For live values (health, energy, stamina, inventory, location, status effects), prefer live MCP/game telemetry over any examples in this file.
- For navigation, ask whether the player is in Story, Survival, or Spirits of Amazonia. Maps and map-fragment placements vary by mode.
- Green Hell uses west (W) and south (S) map coordinates. For example, `46W 26S` means 46 degrees west, 26 degrees south **on the game's coordinate grid**, not real-world GPS.
- For quests, avoid unsolicited spoilers. Offer a hint first, then detailed objectives if requested.
- For crafting, distinguish inventory crafting recipes from notebook construction blueprints. Blueprint availability may depend on discovery and game mode.
- Never claim a blueprint, ingredient, mission objective, or coordinate is verified if this document says it is uncertain.
- The model should answer from relevant retrieved chunks, not from the entire document at once.

## 01 — Core game modes

### Story Mode

Story Mode follows Jake's search for Mia and the events surrounding an expedition in the Amazon rainforest. After the tutorial, the notebook's early objective is to find Mia. Exploration, survival, discovered documents, ritual sequences, and key locations progress the narrative. Some later story sections and endings depend on discoveries and player actions. Story-specific areas and progression items are not interchangeable with Survival Mode.

### Survival Mode

Survival Mode focuses on sustaining the character, establishing a camp, exploring, and managing resources. It is not the same as completing Story Mode's quest sequence. A player asking for a story objective while in Survival Mode should be told that the objective may not exist in that mode.

### Spirits of Amazonia (SoA)

Spirits of Amazonia is a prequel campaign, available separately. It emphasizes exploring additional regions and building trust with tribes through tasks and discoveries. Early objectives include finding the boat and meeting a masked elder. Tribal trust and legends are central progression systems. The locations and maps differ from the main Story campaign.

## 02 — Basic survival loop

1. Inspect the character's condition and watch readings.
2. Establish a reliable source of clean drinking water.
3. Gather varied food for carbohydrates, fats, and protein.
4. Create a safe place to sleep; use a shelter or save point as needed.
5. Keep a basic supply of bandages and inspect unusual status icons.
6. Explore methodically, noting coordinates and landmarks.
7. Cook food and boil water when possible; protect fires from rain.
8. Avoid overcommitting to long travel while energy, hydration, or health is low.

The best next action depends on live stats, nearby resources, inventory, and mode; the knowledge base alone cannot determine those values.

## 03 — Watch, map, notebook, and interface

### Smart Watch

The watch helps the player assess survival indicators, including the four macronutrient/hydration categories. Press `F` to view it and cycle through modes. The compass/GPS display is useful for navigation because the paper map does not automatically mark the player's current position.

### Map

Press `M` after obtaining a map. Right mouse button zooms the map. Different fragments cover different regions. Use the watch coordinates to locate yourself manually. A player who has not discovered a map cannot rely on its display yet.

### Notebook

The notebook tracks discovered building blueprints, observations, and, in story-oriented modes, objectives. A blueprint can be missing because the player has not encountered the required structure or trigger.

### Inventory and crafting

Open the inventory with `Tab` and use its crafting interface; the radial crafting menu can also be opened using `C`. Inventory crafting uses a crafting stone to combine components. Building construction instead uses notebook blueprints placed into the world, then supplied with materials.

## 04 — Vital signs and interpretation

### Health (HP)

Health indicates the character's condition. It can be affected by injuries, untreated illness, and survival problems. The game's current maximum HP can vary; do not assume the denominator is always 100. Prefer `current/max` from live telemetry.

### Energy

Energy is spent through activity and restored principally through rest and other gameplay effects. A safe sleeping spot helps recovery. Some conditions, such as fever, increase the rate of energy loss. Low energy makes extended exploration a poor choice.

### Stamina

Stamina reflects immediate exertion and can fluctuate rapidly while moving. Its effective maximum may track the current energy level; do not present stamina as always being out of 100. A live value like `52/64` is meaningful as a fraction of its current effective maximum.

### Hydration

Hydration decreases over time. Drinking untreated river or pond water risks parasites. Safer options include coconut water, collected rainwater, water collectors/filters, and water boiled in a vessel at a fire.

### Carbohydrates

Carbohydrates come from various fruits and plant foods. Bananas are a common early-game option. Watch the carbohydrate indicator rather than assuming all food addresses all nutritional needs.

### Fats

Nuts and coconuts can supply fats; some animal foods also contain fats. A player who has eaten fruit and protein but still has low fats should seek an appropriate fat source.

### Protein

Fish and cooked animal foods are common protein sources. Some other consumables supply smaller amounts. Cooking is preferable to eating raw meat because raw food can introduce risks.

### Sanity

Sanity can deteriorate because of adverse conditions such as leeches and certain ailments. Rest, suitable food, soups, and the comfort of a camp/fire can help. Extremely low sanity can cause disturbing hallucinations and gameplay hazards. Sanity is separate from HP and energy.

### Hygiene / dirt

Dirt appears as a separate status and matters for infection risk. Washing before treating wounds or eating is a useful precaution where possible.

## 05 — Water: practical progression

### Immediate water options

- Drink the liquid from an unopened coconut if available.
- After consuming coconut flesh, place empty coconut half-shells outdoors to collect rainwater.
- When carrying a container, fill it with water and purify it before drinking where possible.
- Untreated water from rivers or ponds is a fallback with parasite risk, not a default recommendation.

### Boiling water

1. Build and light a fire.
2. Put an empty coconut bowl or other suitable cooking vessel on a cooking slot.
3. Transfer dirty/unsafe water from a bidon into the vessel.
4. Wait until the water boils and becomes safe.
5. Drink from the vessel or transfer clean water to an appropriate container.

Be careful not to pour water onto the fire itself; that can extinguish it.

### Longer-term water systems

Water collectors gather moisture/rainwater; filters clean dirty water. Their blueprints may need to be discovered. Use collectors when a camp is exposed to rain and filters for a more controlled supply. Rain collection can be unreliable during dry periods.

## 06 — Cooking and food storage

### Basic cooking

A lit fire provides cooking slots. Put appropriate food on a slot, wait for its cooking timer to complete, and retrieve/eat it. Some smaller creatures can be cooked whole; larger animals may require processing first. Cooking skill improves through cooking and can shorten cooking time.

### Soups

Cooking vessels can be used for soup. Soup provides the nutrition/effects of its ingredients plus hydration from the water in the vessel. Some ingredients have different effects when cooked into soup compared with being eaten raw. Do not assume every unknown mushroom is safe.

### Food spoilage

Many foods deteriorate with time. Cooked, smoked, and dried foods can have different keeping times. A storage plan matters on longer trips. Never assume a food item is fresh without inventory status.

### Fire and weather

An unprotected fire may go out in rain. A roofed cooking area reduces interruptions. A small fire has two cooking positions and a campfire has four. Fuel can be added to extend burn time.

## 07 — Health conditions and in-game first aid

> These are **Green Hell gameplay mechanics**, not real-world medical advice.

### Inspection

When an inspection icon appears, inspect each limb to identify leeches, rashes, worms, or wounds. An icon by itself may not identify the precise issue.

### Leeches

Leeches attach while traveling in the jungle/water. They reduce sanity over time. Inspect limbs and manually remove each visible leech.

### Scratches and abrasions

These can cause health loss. A basic leaf bandage may help, although clean dressings and hygiene are preferable. Dirty conditions raise infection concerns.

### Lacerations

These are more serious wounds and may bleed or become infected. Better dressings such as ash or honey dressings are preferable to a plain leaf bandage where available. Inspect and treat promptly.

### Infected wounds

The game's healing guide describes using maggots on infected wounds, followed by proper wound care. Fever may accompany infection. This is a game-only mechanic.

### Worms

The game depicts worms as an ailment that requires inspection and removal with a bone needle or fish bone, then a bandage. Do not confuse this with intestinal parasites.

### Rash

A rash can affect sanity. Honey or lily dressing can be used; some rashes also fade over time.

### Venom status

Venom can reduce health and cause fever. In-game tobacco/lily-based dressings or brews are associated with treating venom. Monitor health and the status icon; not every treatment works instantly.

### Fever

Fever accelerates energy and hydration loss. Certain in-game brews reduce fever, but the underlying cause may need treatment. Encourage rest, clean water, and inspection of other active conditions.

### Parasites / food poisoning

Unsafe water and unsuitable food can cause ailments. Prevention is to boil/filter water and use known safe, properly prepared foods. Exact parasite-clearing items and numerical effects should be validated against the installed game version before the assistant gives item-specific claims.

## 08 — Useful non-combat inventory crafting recipes

> These are item crafting combinations, not building costs. Ingredient names follow game terminology. Do not replace exact quantities with guesses.

| Item | Exact ingredients | Use |
|---|---|---|
| Leaf Bandage | Molineria Leaf ×1 | Basic wound dressing |
| Ash Dressing | Leaf Bandage ×1 + Ash ×1 | Improved wound dressing |
| Tobacco Dressing | Leaf Bandage ×1 + Tobacco Leaf ×1 | Dressing associated with venom care |
| Honey Dressing | Leaf Bandage ×1 + Honeycomb ×1 | Dressing for wounds/rashes |
| Lily Dressing | Leaf Bandage ×1 + Plantain Lily Leaf ×1 | Dressing for rashes/venom |
| Goliath Dressing | Leaf Bandage ×1 + Goliath Birdeater Ash ×1 | Advanced dressing |
| Coconut Bidon | Drinkable Coconut ×1 + Rope ×1 | Portable water container |
| Hand Drill | Stick ×1 + Small Stick ×1 | Fire-starting tool |
| Hand Drill Plank | Plank ×1 + Small Stick ×1 | Alternative fire-starting tool |
| Weak Torch | Stick ×1 + Rope ×1 | Portable light |
| Torch | Stick ×1 + Rope ×1 + Tree Resin ×1 | Improved light |

**Fire-starting note:** A hand drill needs suitable tinder such as dry leaves or a bird nest to ignite a prepared fire. Wet weather complicates fire maintenance.

## 09 — Building and construction materials

> Building recipes are assembled at placed blueprints, not on the inventory crafting stone.

| Construction | Required materials | Gameplay purpose |
|---|---|---|
| Leaf Bed | Palm Leaf ×10 | Sleep |
| Banana Leaf Bed | Banana Leaf ×10 | Sleep |
| Log Bed | Log ×4 + Stick ×8 + Long Stick ×6 + Palm Leaf ×8 | Raised sleeping place |
| Bamboo Log Bed | Bamboo Log ×4 + Bamboo Stick ×8 + Long Bamboo Stick ×6 + Palm Leaf ×8 | Bamboo bed |
| Small Fire | Small Stick ×6 + Stick ×8 | Two cooking slots |
| Grill Rack | Long Stick ×6 + Rope ×2 | Additional cooking surface |
| Smoker | Long Stick ×3 + Small Stick ×6 + Rope ×3 + Banana Leaf ×3 | Smoke food |
| Bamboo Smoker | Long Bamboo Stick ×4 + Bamboo Stick ×14 + Rope ×6 + Banana Leaf ×5 | Smoke food |
| Dryer | Long Stick ×7 + Rope ×11 + Small Stick ×11 + Bone Needle ×3 | Dry food |
| Bamboo Dryer | Long Bamboo Stick ×3 + Bamboo Stick ×14 + Rope ×7 | Dry food |
| Water Collector | Long Stick ×4 + Stick ×4 + Rope ×4 + Banana Leaf ×11 | Collect water |
| Bamboo Water Collector | Long Bamboo Stick ×4 + Bamboo Stick ×4 + Rope ×4 + Banana Leaf ×11 | Collect water |
| Water Filter | Long Stick ×2 + Bamboo Log ×1 + Rope ×2 + Charcoal ×3 + Stone ×2 | Filter water |
| Bamboo Water Filter | Long Bamboo Stick ×2 + Bamboo Log ×1 + Rope ×2 + Charcoal ×3 + Stone ×2 | Filter water |
| Mud Water Filter | Long Stick ×8 + Mud Brick ×5 + Charcoal ×6 + Small Stone ×8 + Big Stone ×3 + Bamboo Stick ×1 | Advanced water filtration |
| Mud Shower | Log ×4 + Long Stick ×6 + Rope ×4 + Mud Brick ×8 + Bamboo Stick ×1 | Washing |
| Mud Charcoal Furnace | Mud Brick ×3 | Produce charcoal |

### Shelter distinction

A sleeping bed does not automatically function as a save point. Certain shelters provide save points, and some shelters provide both sleep and save functionality. Consult the blueprint's description. Not all shelter blueprints are available immediately.

### Fire considerations

A small fire burns for approximately 12 in-game hours under its ordinary conditions; a campfire lasts longer, approximately 20 in-game hours. Rain can extinguish uncovered fires. Campfires may attract unwanted attention, so choose camp sites thoughtfully.

## 10 — Plants, food, and resources: recognition

### Coconut

Useful for hydration, edible flesh, bowls, and a portable water container. Coconut flesh supplies some carbohydrates and fats. An empty half-shell can collect rainwater.

### Banana

Useful carbohydrate food. Bananas do not replace fats, protein, or drinking water.

### Nuts

A useful fat source. Specific nuts differ in nutritional value. Some can be carried for travel food.

### Molineria

Its leaves make the basic Leaf Bandage and are important for many improved dressings. Gather a few when exploring.

### Tobacco plant

Tobacco leaf is an ingredient for the in-game tobacco dressing associated with venom management.

### Plantain lily

Used in a lily dressing. Useful in the game's wound/venom treatment system.

### Charcoal and ash

Fire by-products. Charcoal is used in certain water filters; ash is used in ash dressing. Do not conflate them: they are distinct inventory resources.

### Rope

A common ingredient in many tools, water systems, and structures. Check whether the inventory contains enough rope before planning a multi-step build.

### Bamboo and wood

The game distinguishes sticks, small sticks, long sticks, logs, bamboo sticks, long bamboo sticks, and bamboo logs. These are not freely interchangeable in recipes.

## 11 — Map fundamentals and region navigation

### Finding map fragments

**Story Mode:** First map at Abandoned Tribal Village; second at Airport; third at Omega Camp.

**Survival Mode:** Map fragments are associated with Drug Facility, Airport, Omega Camp, Steamboat, and Main Village.

**Spirits of Amazonia:** Map fragments are associated with Steamboat, Drug Facility, Airport (multiple fragments), and Jake's Tent.

Map fragment count and region coverage differ by mode. A map fragment from another mode is not proof of where to find one in the current campaign.

### Coordinates and landmarks

The following are **approximate in-game navigation coordinates** from published location listings. Use the location name and the player's own compass to confirm. Some positions may vary with game version or mode.

| Landmark | Coordinate | Description |
|---|---|---|
| Abandoned Tribal Village | 46W 26S | Early Story landmark; Story Map 1 |
| Drug Facility | 51W 27S | Settlement/abandoned facility; Survival map location |
| Fishing Dock | 51W 19S | Waterside settlement |
| Overturned Jeep | 44W 17S | Recognizable landmark and story-related exploration point |
| Anaconda Island / Camp | 35W 25–26S | Camp/landmark in the eastern region; sources vary by one grid unit |
| Airport | 29W 22S | Major story/exploration location; map fragment |
| Bamboo Bridge | 37W 20S | Route landmark between regions |
| East Native Camp | 37W 19S | Native camp landmark |
| West Native Camp | 47W 19S | Native camp landmark |
| Small Native Camp | 35W 19S | Native camp landmark |
| Omega Camp | 25W 33S | Research facility; Story Map 3 |
| Plane Crash Cave | about 40W 24S | Cave / crash landmark; approximate |
| Elevator | about 40W 18S | Region traversal landmark; approximate |

### Other named places

Other notable locations include Bamboo Camp, Delta Camp, Illegal Gold Mine, Jake's Tent, Main Village, Mu'agi Village, Lambda-1, Lambda-2, Steamboat, Waterfall Oasis, Waterhole, Cenote, Alvarez Cave, Oasis Cave, Scorpion Cave, Skull Cave, and underwater caves. **This document does not contain independently confirmed coordinates for all of them.** Never invent an exact coordinate to complete an answer.

### Navigation troubleshooting

- If a player says "I am lost," ask for their watch coordinates and mode.
- If the player has a map, help compare coordinates to known landmarks.
- If the player asks for the nearest location, do not compute distance from approximate coordinates without accounting for impassable terrain, rivers, caves, and access requirements.
- Some routes require story-specific traversal equipment or progress; a straight line on the map is not necessarily walkable.

## 12 — Main Story progression (SPOILERS)

> `spoiler_level: major`. Do not retrieve or expose this section unless the player asks for story/mission help or opts into spoilers.

### Starting objective: Find Mia

After the tutorial, the notebook directs Jake to find Mia. The player explores the jungle, locates evidence and story locations, and uncovers memories. The notebook is the authoritative place for the currently active objective.

### Ayahuasca rituals

Ritual sequences are used to reveal memories and advance narrative understanding. The player gathers ritual ingredients including Caapi vine and a suitable Psychotria viridis plant, then uses a ritual site to prepare the brew. These are **fictional story mechanics**; the assistant should not turn them into real-world preparation advice.

### Grappling-hook progression

Certain traversal points require a grappling hook obtained through exploration of the relevant story locations. The Overturned Jeep at about 44W 17S is a key landmark mentioned in guides. The order of reaching locations can depend on other access steps.

### Airport

The Airport is a major exploration location and the place where Story Map 2 can be found. Search the area for narrative clues and access requirements relevant to the active objective.

### Omega Camp and the research lab

Omega Camp is at about 25W 33S. It has research tents, supplies, a map fragment, and a laboratory. Later in the story, the player investigates a disease outbreak and uses a sample analyzer as part of a cure-related objective. This sequence is not a normal Survival Mode quest.

### Cure and ending variation

The main story contains a cure-related discovery that affects later narrative choices. The laboratory's infected sample and further testing are relevant. The player can progress to a final vision and dialogue; whether the cure was found affects available dialogue/ending outcomes. **Do not spoil the exact solution unless requested.**

### Story-mode warning

Quest progression summaries on community sites sometimes disagree on precise order or coordinates. If the player names an objective from their notebook, prioritize that text and the current game state over an assumed universal quest order.

## 13 — Spirits of Amazonia mission system (SPOILERS)

> `spoiler_level: moderate`.

### Opening

Early tasks include finding the boat and finding the masked elder. These can be explored in either order. Speaking with the elder begins a trust-building arc with the Mu'agi tribe.

### Trust progression

Trust is earned by completing activities relevant to local tribes, which can include helping people, resolving regional tasks, and investigating legends. Tribes and areas have distinct progression; the exact available task depends on the chapter and location.

### Legends

Legends are environmental mysteries and clue-based activities. The player should read discovered notebook entries and inspect nearby landmarks. Avoid inventing an answer to a legend not covered in a verified source.

### Different map regions

SoA includes additional areas and its own map-fragment locations. Do not direct a SoA player to a Story Mode map fragment without checking the mode-specific list.

## 14 — Question/answer patterns for the AI Companion

**Q: I have low energy. What should I do?**
A: Find a safe place to rest, check whether fever or other conditions are accelerating energy loss, and avoid a long expedition until you recover. Check hydration and nutrition too. If a bed is available, use it.

**Q: I am thirsty and have a coconut.**
A: Drink the coconut water. After using the coconut, you can turn its halves into bowls and place them outdoors to collect rainwater. If you have a fire and container, boiling unsafe water is another option.

**Q: Can I drink river water?**
A: You can, but it carries parasite risk. Boil it in a suitable vessel or use a filter if possible.

**Q: How do I craft a bandage?**
A: Use one Molineria Leaf. Improved dressings combine a leaf bandage with an additional ingredient, such as ash or honeycomb.

**Q: Where is the first Story map?**
A: At the Abandoned Tribal Village, around 46W 26S. Open it with M after collecting it; use your watch to work out your position.

**Q: Where is the Omega research camp?**
A: Around 25W 33S, in a later region of the Story map. Access may depend on progression and terrain.

**Q: Why is my stamina maximum only 64?**
A: The effective stamina maximum can follow your current energy level. Read the live maximum from the game instead of assuming stamina is out of 100.

**Q: I have leeches.**
A: Inspect your limbs and remove the attached leeches. They reduce sanity while attached.

**Q: How do I find a missing blueprint?**
A: Explore locations and inspect existing structures. Some construction blueprints unlock only after discovery. Tell me the blueprint name and your game mode for a more precise answer.

**Q: What is my next mission?**
A: I need your current game mode and notebook objective. In Story Mode, objectives relate to finding Mia and subsequent discoveries; in Spirits of Amazonia, tribal trust and legends drive progress.

## 15 — Known gaps and reliability flags

- This document is **not a complete enumeration** of every item, plant, food effect, recipe, animal, building, coordinate, cave, or quest.
- Do not claim precise numerical food effects, spoilage timers, or status cure magnitudes without an independently checked game-data catalog.
- Many wiki entries reflect older patches. The installed Steam game version may differ.
- Map coordinates may vary slightly across sources and game modes.
- Detailed story objectives should be confirmed with the player's notebook, because sequence and available interactions can change with progression.
- `get_crafting_recipe(item_name)` should ultimately read a deterministic verified recipe catalog, rather than asking an LLM to reconstruct ingredient lists.
- `get_player_inventory()` and `get_survival_conditions()` should report actual game state when implemented; this file cannot provide live values.

## 16 — Ingestion instructions (for Codex)

- Split by `##` topic, then `###` subtopic; preserve each recipe table row as a standalone semantic record with its parent heading.
- Attach metadata to each chunk: `game=Green Hell`, `mode=story|survival|soa|all`, `topic`, `spoiler_level`, `verified_runtime=false`, `knowledge_version=2026-10-15`.
- Use Bedrock Titan Text Embeddings V2 and a Qdrant collection with vector size matching the selected embedding dimensions.
- For exact recipes, additionally parse tables into structured deterministic records keyed by item name and aliases. Vector search is for explanation and discovery, not authoritative recipe arithmetic.
- Do not mix spoiler chunks into ordinary survival retrieval by default.
- Test questions covering survival, crafting, maps, quests, SoA, unknown questions, and incorrect game mode.
- When retrieval lacks evidence, the assistant should say it does not have a verified answer.
