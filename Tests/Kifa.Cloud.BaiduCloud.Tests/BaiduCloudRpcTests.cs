using FluentAssertions;
using Kifa.Cloud.BaiduCloud.Rpcs;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Cloud.BaiduCloud.Tests;

public class BaiduCloudRpcTests {
    [Fact]
    public void GetFileInfoRpcDeserializationTest() {
        var json = """
            {
                "errno": 0,
                "errmsg": "succ",
                "request_id": 123456789,
                "list": [
                    {
                        "fs_id": 12345,
                        "path": "/app/test.bin",
                        "size": 1048576,
                        "isdir": 0,
                        "ifhassubdir": 0,
                        "md5": "3dd3601b968aebb08c6fd3e1a66d22c3"
                    }
                ]
            }
            """;

        var response = JsonConvert.DeserializeObject<GetFileInfoRpc.Response>(json,
            KifaJsonSerializerSettings.Default);
        response.Should().NotBeNull();
        response!.Errno.Should().Be(0);
        response.List.Should().NotBeNull();
        response.List.Should().HaveCount(1);
        var item = response.List![0];
        item.Path.Should().Be("/app/test.bin");
        item.Size.Should().Be(1048576);
        item.Isdir.Should().Be(0);
        item.Md5.Should().Be("3dd3601b968aebb08c6fd3e1a66d22c3");
    }

    [Fact]
    public void ListFilesRpcDeserializationTest() {
        var json = """
            {
                "errno": 0,
                "errmsg": "succ",
                "request_id": 123456789,
                "list": [
                    {
                        "fs_id": 12345,
                        "path": "/app/test.bin",
                        "size": 1048576,
                        "isdir": 0,
                        "md5": "3dd3601b968aebb08c6fd3e1a66d22c3"
                    }
                ]
            }
            """;

        var response = JsonConvert.DeserializeObject<ListFilesRpc.Response>(json,
            KifaJsonSerializerSettings.Default);
        response.Should().NotBeNull();
        response!.Errno.Should().Be(0);
        response.List.Should().NotBeNull();
        response.List.Should().HaveCount(1);
        var item = response.List![0];
        item.Path.Should().Be("/app/test.bin");
        item.Size.Should().Be(1048576);
        item.Isdir.Should().Be(0);
        item.Md5.Should().Be("3dd3601b968aebb08c6fd3e1a66d22c3");
    }

    [Fact]
    public void DiffFileListRpcDeserializationTest() {
        var json = """
            {
                "errno": 0,
                "errmsg": "succ",
                "request_id": 123456789,
                "cursor": "cursor_123",
                "has_more": false,
                "reset": false,
                "entries": {
                    "/app/test.bin": {
                        "fs_id": 12345,
                        "path": "/app/test.bin",
                        "size": 1048576,
                        "isdir": 0,
                        "md5": "3dd3601b968aebb08c6fd3e1a66d22c3",
                        "isdelete": 0,
                        "ctime": 1600000000,
                        "mtime": 1600000000
                    }
                }
            }
            """;

        var response = JsonConvert.DeserializeObject<DiffFileListRpc.Response>(json,
            KifaJsonSerializerSettings.Default);
        response.Should().NotBeNull();
        response!.Errno.Should().Be(0);
        response.Cursor.Should().Be("cursor_123");
        response.Entries.Should().NotBeNull();
        response.Entries.Should().ContainKey("/app/test.bin");
        var entry = response.Entries["/app/test.bin"];
        entry.Path.Should().Be("/app/test.bin");
        entry.Size.Should().Be(1048576);
        entry.Md5.Should().Be("3dd3601b968aebb08c6fd3e1a66d22c3");
    }
}
