# World Overview

## Goal

We want to make world's information sorted out with structure, grouped by `World` and easily accessible.

## Definition of World

For each `World`, we want it to focus on single, specific topic, like one game, one language, etc.

Or all TV Shows, or all Anime, which all count as one, as they share very similar nature
of the structure: data, types of resource files etc.

## Naming

We are not sure about world naming yet. It can be that we may want to prepend a category to the actual name like,
`worlds.games.botw` or just `BreathOfTheWild`. But the key is we don't rely on this container name in our data so that
it's easily switchable. For `Link` objects referencing other types, they are only defined in server code instead of
prefixing the item type etc.

### Examples of worlds

#### A Game: The Legend of Zelda: Breath of the Wild

Topic is clear. What we may have these entities:

- Items
- Bosses
- Side quests
...

For each entity, we will have data and resources. For example, for Boss `Hinox`, we may have,

```json
{
  "id": "Hinox",
  "image": "https://www.zeldadungeon.net/wiki/images/a/ac/Hinox-botw.jpg",
  "health": 600,
  "quests": []
}
```

Then for resource, we have the image link in the data, which is both accessible via direct link,
or any source linked to `/Web/https.www.zeldadungeon.net/wiki/images/a/ac/Hinox-botw.jpg`.

Note that data and resources will be individually separated and grouped in its `World`
(but files may be implicitly shared internally). We may want a publicly available storage solution before we can make it
fully independent of our other infra.

#### Collection of similar data: TV Shows

We already have the structure for a TV Show. Overall it will still be like that. But we can have more sources.

So we still have one pool of all TV Shows like

- Agatha Christie's Poirot
- Dark
...

We can then have referenced data like actors, directors, (maybe) companies etc. Resources like cover, banner can also be
collected from websites and stored in World specific locations.

We can also have our source data recorded to get a clearer view. For example, we can log raw tmdb data another pool.
With our `Link<>` structure, the `tmdb_id` field can be replaced by `tmdb`, which can optionally be fully loaded. We can
also control whether we need to refresh TV Show object in the case of whether the `tmdb` object is updated (this is
pending support).

## Data separation

We will definitely separate the information data, but for file storage, it may not be very clear. We don't want to put
them into user (even our default user Kimily)'s sub paths, but default (or other) user may need to access those files.
One way to solve this is to have a separate `WorldClient` that can such data, files etc. It may need some extra work to
support two clients in one app run.

## Linking

One big use case of consuming these data when ready is about personal notes. For example, I've watched this episode, I
want to mark that, or I want to score that. Or I've played this game and beaten this boss, I want to mark it. Or I want
to create a German list in Memrise/Anki and I want to use the sorted out information.

These should all be supported. But is currently pending investigation. One idea would be these data should be visible to
all users like their native data. One issue is file storage separation may be a problem is it's not shared (data can be
controlled by type, but file control by folder is not nicely supported without messing personal folder structure).
