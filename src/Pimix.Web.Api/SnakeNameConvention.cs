using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Pimix.Web.Api {
    public class SnakeNameConvention : IMemberMapConvention {
        static readonly Regex camelCasePattern = new Regex("[A-Z]+[a-z0-9]*");
        public string Name => "Snake Name Convention";

        public void Apply(BsonMemberMap memberMap) {
            var name = memberMap.MemberName;
            memberMap.SetElementName(string.Join("_", camelCasePattern.Matches(name).Select(x => x.Value.ToLower())));
        }
    }
}
