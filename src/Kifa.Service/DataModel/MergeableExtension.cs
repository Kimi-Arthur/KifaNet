using Newtonsoft.Json;

namespace Kifa.Service;

public static class MergeableExtension {
    public static TDataModel Merge<TDataModel>(this TDataModel data, TDataModel update) {
        var obj = data.Clone();
        JsonConvert.PopulateObject(
            JsonConvert.SerializeObject(update, KifaJsonSerializerSettings.Default), obj!,
            KifaJsonSerializerSettings.Merge);
        return obj;
    }
}
