# Customization Rules for KifaNet

## Baidu Cloud Migration Rules
- During the migration of Baidu Cloud (or other PCS/Cloud APIs) from JToken-based responses to structured RPC response models, pay close attention to fields containing collections of items (like `entries` in file lists or diff lists). Some APIs return a JSON object/map (dictionary) where the keys are paths or IDs, rather than a JSON array (list). Ensure these are deserialized as `Dictionary<string, T>` and not `List<T>`.
