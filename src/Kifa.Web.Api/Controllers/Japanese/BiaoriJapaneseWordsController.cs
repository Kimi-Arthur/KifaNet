using Kifa.Languages.Japanese;
using Kifa.Web.Api.JsonClients.Japanese;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Japanese;

public class
    BiaoriJapaneseWordsController : KifaDataController<BiaoriJapaneseWord,
        BiaoriJapaneseWordJsonClient> {
}
