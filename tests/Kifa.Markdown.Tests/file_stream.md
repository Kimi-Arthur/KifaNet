# FileStream Class

## Definition

- Namespace: [System.IO](https://docs.microsoft.com/en-us/dotnet/api/system.io?view=net-5.0)

- Assembly: System.Runtime.dll

Provides a [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0) for a file, supporting both synchronous and asynchronous read and write operations.

C#Copy

```csharp
public class FileStream : System.IO.Stream
```

- Inheritance: [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)[MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0)[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)FileStream

- Derived: [System.IO.IsolatedStorage.IsolatedStorageFileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.isolatedstorage.isolatedstoragefilestream?view=net-5.0)

## Examples

The following example demonstrates some of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) constructors.

C#Copy

```csharp
using System;
using System.IO;
using System.Text;

class Test
{

    public static void Main()
    {
        string path = @"c:\temp\MyTest.txt";

        // Delete the file if it exists.
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        //Create the file.
        using (FileStream fs = File.Create(path))
        {
            AddText(fs, "This is some text");
            AddText(fs, "This is some more text,");
            AddText(fs, "\r\nand this is on a new line");
            AddText(fs, "\r\n\r\nThe following is a subset of characters:\r\n");

            for (int i=1;i < 120;i++)
            {
                AddText(fs, Convert.ToChar(i).ToString());
            }
        }

        //Open the stream and read it back.
        using (FileStream fs = File.OpenRead(path))
        {
            byte[] b = new byte[1024];
            UTF8Encoding temp = new UTF8Encoding(true);
            while (fs.Read(b,0,b.Length) > 0)
            {
                Console.WriteLine(temp.GetString(b));
            }
        }
    }

    private static void AddText(FileStream fs, string value)
    {
        byte[] info = new UTF8Encoding(true).GetBytes(value);
        fs.Write(info, 0, info.Length);
    }
}
```

The following example shows how to write to a file asynchronously. This code runs in a WPF app that has a TextBlock named UserInput and a button hooked up to a Click event handler that is named Button_Click. The file path needs to be changed to a file that exists on the computer.

C#Copy

```csharp
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            UnicodeEncoding uniencoding = new UnicodeEncoding();
            string filename = @"c:\Users\exampleuser\Documents\userinputlog.txt";

            byte[] result = uniencoding.GetBytes(UserInput.Text);

            using (FileStream SourceStream = File.Open(filename, FileMode.OpenOrCreate))
            {
                SourceStream.Seek(0, SeekOrigin.End);
                await SourceStream.WriteAsync(result, 0, result.Length);
            }
        }
    }
}
```

## Remarks

Use the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class to read from, write to, open, and close files on a file system, and to manipulate other file-related operating system handles, including pipes, standard input, and standard output. You can use the [Read](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.read?view=net-5.0), [Write](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.write?view=net-5.0), [CopyTo](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copyto?view=net-5.0), and [Flush](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-5.0) methods to perform synchronous operations, or the [ReadAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0), [WriteAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0), [CopyToAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=net-5.0), and [FlushAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.flushasync?view=net-5.0) methods to perform asynchronous operations. Use the asynchronous methods to perform resource-intensive file operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) buffers input and output for better performance.

> [!IMPORTANT]
>
> This type implements the [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-5.0) interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its [Dispose](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable.dispose?view=net-5.0) method in a `try`/`catch` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-5.0) interface topic.
>
> The [IsAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.isasync?view=net-5.0) property detects whether the file handle was opened asynchronously. You specify this value when you create an instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class using a constructor that has an `isAsync`, `useAsync`, or `options` parameter. When the property is `true`, the stream utilizes overlapped I/O to perform file operations asynchronously. However, the [IsAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.isasync?view=net-5.0) property does not have to be `true` to call the [ReadAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0), [WriteAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0), or [CopyToAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=net-5.0) method. When the [IsAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.isasync?view=net-5.0) property is `false` and you call the asynchronous read and write operations, the UI thread is still not blocked, but the actual I/O operation is performed synchronously.
>
> The [Seek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.seek?view=net-5.0) method supports random access to files. [Seek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.seek?view=net-5.0) allows the read/write position to be moved to any position within the file. This is done with byte offset reference point parameters. The byte offset is relative to the seek reference point, which can be the beginning, the current position, or the end of the underlying file, as represented by the three members of the [SeekOrigin](https://docs.microsoft.com/en-us/dotnet/api/system.io.seekorigin?view=net-5.0) enumeration.

> [!NOTE]
>
> Disk files always support random access. At the time of construction, the [CanSeek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canseek?view=net-5.0) property value is set to `true` or `false` depending on the underlying file type. If the underlying file type is FILE_TYPE_DISK, as defined in winbase.h, the [CanSeek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canseek?view=net-5.0) property value is `true`. Otherwise, the [CanSeek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canseek?view=net-5.0) property value is `false`.
>
> If a process terminates with part of a file locked or closes a file that has outstanding locks, the behavior is undefined.
>
> For directory operations and other file operations, see the [File](https://docs.microsoft.com/en-us/dotnet/api/system.io.file?view=net-5.0), [Directory](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory?view=net-5.0), and [Path](https://docs.microsoft.com/en-us/dotnet/api/system.io.path?view=net-5.0) classes. The [File](https://docs.microsoft.com/en-us/dotnet/api/system.io.file?view=net-5.0) class is a utility class that has static methods primarily for the creation of [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) objects based on file paths. The [MemoryStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.memorystream?view=net-5.0) class creates a stream from a byte array and is similar to the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class.
>
> For a list of common file and directory operations, see [Common I/O Tasks](https://docs.microsoft.com/en-us/dotnet/standard/io/common-i-o-tasks).

### Detection of Stream Position Changes

When a [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object does not have an exclusive hold on its handle, another thread could access the file handle concurrently and change the position of the operating system's file pointer that is associated with the file handle. In this case, the cached position in the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object and the cached data in the buffer could be compromised. The [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object routinely performs checks on methods that access the cached buffer to ensure that the operating system's handle position is the same as the cached position used by the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object.

If an unexpected change in the handle position is detected in a call to the [Read](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.read?view=net-5.0) method, the .NET Framework discards the contents of the buffer and reads the stream from the file again. This can affect performance, depending on the size of the file and any other processes that could affect the position of the file stream.

If an unexpected change in the handle position is detected in a call to the [Write](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.write?view=net-5.0) method, the contents of the buffer are discarded and an [IOException](https://docs.microsoft.com/en-us/dotnet/api/system.io.ioexception?view=net-5.0) exception is thrown.

A [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object will not have an exclusive hold on its handle when either the [SafeFileHandle](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.safefilehandle?view=net-5.0) property is accessed to expose the handle or the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object is given the [SafeFileHandle](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.safefilehandle?view=net-5.0) property in its constructor.

## Constructors

| [FileStream(IntPtr, FileAccess)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_IntPtr_System_IO_FileAccess_) | **Obsolete.**Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission. |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [FileStream(IntPtr, FileAccess, Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_IntPtr_System_IO_FileAccess_System_Boolean_) | **Obsolete.**Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission and `FileStream` instance ownership. |
| [FileStream(IntPtr, FileAccess, Boolean, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_IntPtr_System_IO_FileAccess_System_Boolean_System_Int32_) | **Obsolete.**Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission, `FileStream` instance ownership, and buffer size. |
| [FileStream(IntPtr, FileAccess, Boolean, Int32, Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_IntPtr_System_IO_FileAccess_System_Boolean_System_Int32_System_Boolean_) | **Obsolete.**Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission, `FileStream` instance ownership, buffer size, and synchronous or asynchronous state. |
| [FileStream(SafeFileHandle, FileAccess)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_Microsoft_Win32_SafeHandles_SafeFileHandle_System_IO_FileAccess_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission. |
| [FileStream(SafeFileHandle, FileAccess, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_Microsoft_Win32_SafeHandles_SafeFileHandle_System_IO_FileAccess_System_Int32_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission, and buffer size. |
| [FileStream(SafeFileHandle, FileAccess, Int32, Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_Microsoft_Win32_SafeHandles_SafeFileHandle_System_IO_FileAccess_System_Int32_System_Boolean_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class for the specified file handle, with the specified read/write permission, buffer size, and synchronous or asynchronous state. |
| [FileStream(String, FileMode)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path and creation mode. |
| [FileStream(String, FileMode, FileAccess)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_System_IO_FileAccess_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path, creation mode, and read/write permission. |
| [FileStream(String, FileMode, FileAccess, FileShare)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_System_IO_FileAccess_System_IO_FileShare_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path, creation mode, read/write permission, and sharing permission. |
| [FileStream(String, FileMode, FileAccess, FileShare, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_System_IO_FileAccess_System_IO_FileShare_System_Int32_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path, creation mode, read/write and sharing permission, and buffer size. |
| [FileStream(String, FileMode, FileAccess, FileShare, Int32, Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_System_IO_FileAccess_System_IO_FileShare_System_Int32_System_Boolean_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path, creation mode, read/write and sharing permission, buffer size, and synchronous or asynchronous state. |
| [FileStream(String, FileMode, FileAccess, FileShare, Int32, FileOptions)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.-ctor?view=net-5.0#System_IO_FileStream__ctor_System_String_System_IO_FileMode_System_IO_FileAccess_System_IO_FileShare_System_Int32_System_IO_FileOptions_) | Initializes a new instance of the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) class with the specified path, creation mode, read/write and sharing permission, the access other FileStreams can have to the same file, the buffer size, and additional file options. |

## Properties

| [CanRead](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canread?view=net-5.0#System_IO_FileStream_CanRead) | Gets a value that indicates whether the current stream supports reading. |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [CanSeek](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canseek?view=net-5.0#System_IO_FileStream_CanSeek) | Gets a value that indicates whether the current stream supports seeking. |
| [CanTimeout](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.cantimeout?view=net-5.0#System_IO_Stream_CanTimeout) | Gets a value that determines whether the current stream can time out.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CanWrite](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.canwrite?view=net-5.0#System_IO_FileStream_CanWrite) | Gets a value that indicates whether the current stream supports writing. |
| [Handle](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.handle?view=net-5.0#System_IO_FileStream_Handle) | **Obsolete.**Gets the operating system file handle for the file that the current `FileStream` object encapsulates. |
| [IsAsync](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.isasync?view=net-5.0#System_IO_FileStream_IsAsync) | Gets a value that indicates whether the `FileStream` was opened asynchronously or synchronously. |
| [Length](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.length?view=net-5.0#System_IO_FileStream_Length) | Gets the length in bytes of the stream.                      |
| [Name](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.name?view=net-5.0#System_IO_FileStream_Name) | Gets the absolute path of the file opened in the `FileStream`. |
| [Position](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.position?view=net-5.0#System_IO_FileStream_Position) | Gets or sets the current position of this stream.            |
| [ReadTimeout](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.readtimeout?view=net-5.0#System_IO_Stream_ReadTimeout) | Gets or sets a value, in milliseconds, that determines how long the stream will attempt to read before timing out.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [SafeFileHandle](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.safefilehandle?view=net-5.0#System_IO_FileStream_SafeFileHandle) | Gets a [SafeFileHandle](https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.safehandles.safefilehandle?view=net-5.0) object that represents the operating system file handle for the file that the current [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) object encapsulates. |
| [WriteTimeout](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.writetimeout?view=net-5.0#System_IO_Stream_WriteTimeout) | Gets or sets a value, in milliseconds, that determines how long the stream will attempt to write before timing out.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |

## Methods

| [BeginRead(Byte[\], Int32, Int32, AsyncCallback, Object)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.beginread?view=net-5.0#System_IO_FileStream_BeginRead_System_Byte___System_Int32_System_Int32_System_AsyncCallback_System_Object_) | Begins an asynchronous read operation. Consider using [ReadAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0#System_IO_FileStream_ReadAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) instead. |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [BeginWrite(Byte[\], Int32, Int32, AsyncCallback, Object)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.beginwrite?view=net-5.0#System_IO_FileStream_BeginWrite_System_Byte___System_Int32_System_Int32_System_AsyncCallback_System_Object_) | Begins an asynchronous write operation. Consider using [WriteAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0#System_IO_FileStream_WriteAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) instead. |
| [Close()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.close?view=net-5.0#System_IO_Stream_Close) | Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream. Instead of calling this method, ensure that the stream is properly disposed.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyTo(Stream)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copyto?view=net-5.0#System_IO_Stream_CopyTo_System_IO_Stream_) | Reads the bytes from the current stream and writes them to another stream.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyTo(Stream, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copyto?view=net-5.0#System_IO_Stream_CopyTo_System_IO_Stream_System_Int32_) | Reads the bytes from the current stream and writes them to another stream, using a specified buffer size.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyToAsync(Stream)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=net-5.0#System_IO_Stream_CopyToAsync_System_IO_Stream_) | Asynchronously reads the bytes from the current stream and writes them to another stream.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyToAsync(Stream, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=net-5.0#System_IO_Stream_CopyToAsync_System_IO_Stream_System_Threading_CancellationToken_) | Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified cancellation token.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyToAsync(Stream, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=net-5.0#System_IO_Stream_CopyToAsync_System_IO_Stream_System_Int32_) | Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified buffer size.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [CopyToAsync(Stream, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.copytoasync?view=net-5.0#System_IO_FileStream_CopyToAsync_System_IO_Stream_System_Int32_System_Threading_CancellationToken_) | Asynchronously reads the bytes from the current file stream and writes them to another stream, using a specified buffer size and cancellation token. |
| [CreateObjRef(Type)](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject.createobjref?view=net-5.0#System_MarshalByRefObject_CreateObjRef_System_Type_) | Creates an object that contains all the relevant information required to generate a proxy used to communicate with a remote object.(Inherited from [MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0)) |
| [CreateWaitHandle()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.createwaithandle?view=net-5.0#System_IO_Stream_CreateWaitHandle) | **Obsolete.**Allocates a [WaitHandle](https://docs.microsoft.com/en-us/dotnet/api/system.threading.waithandle?view=net-5.0) object.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [Dispose()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.dispose?view=net-5.0#System_IO_Stream_Dispose) | Releases all resources used by the [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0).(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [Dispose(Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.dispose?view=net-5.0#System_IO_FileStream_Dispose_System_Boolean_) | Releases the unmanaged resources used by the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0) and optionally releases the managed resources. |
| [DisposeAsync()](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.disposeasync?view=net-5.0#System_IO_FileStream_DisposeAsync) | Asynchronously releases the unmanaged resources used by the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0). |
| [EndRead(IAsyncResult)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.endread?view=net-5.0#System_IO_FileStream_EndRead_System_IAsyncResult_) | Waits for the pending asynchronous read operation to complete. (Consider using [ReadAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0#System_IO_FileStream_ReadAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) instead.) |
| [EndWrite(IAsyncResult)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.endwrite?view=net-5.0#System_IO_FileStream_EndWrite_System_IAsyncResult_) | Ends an asynchronous write operation and blocks until the I/O operation is complete. (Consider using [WriteAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0#System_IO_FileStream_WriteAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) instead.) |
| [Equals(Object)](https://docs.microsoft.com/en-us/dotnet/api/system.object.equals?view=net-5.0#System_Object_Equals_System_Object_) | Determines whether the specified object is equal to the current object.(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)) |
| [Finalize()](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.finalize?view=net-5.0#System_IO_FileStream_Finalize) | Ensures that resources are freed and other cleanup operations are performed when the garbage collector reclaims the `FileStream`. |
| [Flush()](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-5.0#System_IO_FileStream_Flush) | Clears buffers for this stream and causes any buffered data to be written to the file. |
| [Flush(Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-5.0#System_IO_FileStream_Flush_System_Boolean_) | Clears buffers for this stream and causes any buffered data to be written to the file, and also clears all intermediate file buffers. |
| [FlushAsync()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.flushasync?view=net-5.0#System_IO_Stream_FlushAsync) | Asynchronously clears all buffers for this stream and causes any buffered data to be written to the underlying device.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [FlushAsync(CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.flushasync?view=net-5.0#System_IO_FileStream_FlushAsync_System_Threading_CancellationToken_) | Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests. |
| [GetHashCode()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=net-5.0#System_Object_GetHashCode) | Serves as the default hash function.(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)) |
| [GetLifetimeService()](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject.getlifetimeservice?view=net-5.0#System_MarshalByRefObject_GetLifetimeService) | **Obsolete.**Retrieves the current lifetime service object that controls the lifetime policy for this instance.(Inherited from [MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0)) |
| [GetType()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gettype?view=net-5.0#System_Object_GetType) | Gets the [Type](https://docs.microsoft.com/en-us/dotnet/api/system.type?view=net-5.0) of the current instance.(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)) |
| [InitializeLifetimeService()](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject.initializelifetimeservice?view=net-5.0#System_MarshalByRefObject_InitializeLifetimeService) | **Obsolete.**Obtains a lifetime service object to control the lifetime policy for this instance.(Inherited from [MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0)) |
| [Lock(Int64, Int64)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.lock?view=net-5.0#System_IO_FileStream_Lock_System_Int64_System_Int64_) | Prevents other processes from reading from or writing to the [FileStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0). |
| [MemberwiseClone()](https://docs.microsoft.com/en-us/dotnet/api/system.object.memberwiseclone?view=net-5.0#System_Object_MemberwiseClone) | Creates a shallow copy of the current [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0).(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)) |
| [MemberwiseClone(Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject.memberwiseclone?view=net-5.0#System_MarshalByRefObject_MemberwiseClone_System_Boolean_) | Creates a shallow copy of the current [MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0) object.(Inherited from [MarshalByRefObject](https://docs.microsoft.com/en-us/dotnet/api/system.marshalbyrefobject?view=net-5.0)) |
| [ObjectInvariant()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.objectinvariant?view=net-5.0#System_IO_Stream_ObjectInvariant) | **Obsolete.**Provides support for a [Contract](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.contracts.contract?view=net-5.0).(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [Read(Byte[\], Int32, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.read?view=net-5.0#System_IO_FileStream_Read_System_Byte___System_Int32_System_Int32_) | Reads a block of bytes from the stream and writes the data in a given buffer. |
| [Read(Span)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.read?view=net-5.0#System_IO_FileStream_Read_System_Span_System_Byte__) | Reads a sequence of bytes from the current file stream and advances the position within the file stream by the number of bytes read. |
| [ReadAsync(Byte[\], Int32, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.readasync?view=net-5.0#System_IO_Stream_ReadAsync_System_Byte___System_Int32_System_Int32_) | Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [ReadAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0#System_IO_FileStream_ReadAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) | Asynchronously reads a sequence of bytes from the current file stream and writes them to a byte array beginning at a specified offset, advances the position within the file stream by the number of bytes read, and monitors cancellation requests. |
| [ReadAsync(Memory, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync?view=net-5.0#System_IO_FileStream_ReadAsync_System_Memory_System_Byte__System_Threading_CancellationToken_) | Asynchronously reads a sequence of bytes from the current file stream and writes them to a memory region, advances the position within the file stream by the number of bytes read, and monitors cancellation requests. |
| [ReadByte()](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.readbyte?view=net-5.0#System_IO_FileStream_ReadByte) | Reads a byte from the file and advances the read position one byte. |
| [Seek(Int64, SeekOrigin)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.seek?view=net-5.0#System_IO_FileStream_Seek_System_Int64_System_IO_SeekOrigin_) | Sets the current position of this stream to the given value. |
| [SetLength(Int64)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.setlength?view=net-5.0#System_IO_FileStream_SetLength_System_Int64_) | Sets the length of this stream to the given value.           |
| [ToString()](https://docs.microsoft.com/en-us/dotnet/api/system.object.tostring?view=net-5.0#System_Object_ToString) | Returns a string that represents the current object.(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object?view=net-5.0)) |
| [Unlock(Int64, Int64)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.unlock?view=net-5.0#System_IO_FileStream_Unlock_System_Int64_System_Int64_) | Allows access by other processes to all or part of a file that was previously locked. |
| [Write(Byte[\], Int32, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.write?view=net-5.0#System_IO_FileStream_Write_System_Byte___System_Int32_System_Int32_) | Writes a block of bytes to the file stream.                  |
| [Write(ReadOnlySpan)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.write?view=net-5.0#System_IO_FileStream_Write_System_ReadOnlySpan_System_Byte__) | Writes a sequence of bytes from a read-only span to the current file stream and advances the current position within this file stream by the number of bytes written. |
| [WriteAsync(Byte[\], Int32, Int32)](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.writeasync?view=net-5.0#System_IO_Stream_WriteAsync_System_Byte___System_Int32_System_Int32_) | Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| [WriteAsync(Byte[\], Int32, Int32, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0#System_IO_FileStream_WriteAsync_System_Byte___System_Int32_System_Int32_System_Threading_CancellationToken_) | Asynchronously writes a sequence of bytes to the current stream, advances the current position within this stream by the number of bytes written, and monitors cancellation requests. |
| [WriteAsync(ReadOnlyMemory, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writeasync?view=net-5.0#System_IO_FileStream_WriteAsync_System_ReadOnlyMemory_System_Byte__System_Threading_CancellationToken_) | Asynchronously writes a sequence of bytes from a memory region to the current file stream, advances the current position within this file stream by the number of bytes written, and monitors cancellation requests. |
| [WriteByte(Byte)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream.writebyte?view=net-5.0#System_IO_FileStream_WriteByte_System_Byte_) | Writes a byte to the current position in the file stream.    |

## Explicit Interface Implementations

| [IDisposable.Dispose()](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.system-idisposable-dispose?view=net-5.0#System_IO_Stream_System_IDisposable_Dispose) | Releases all resources used by the [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0).(Inherited from [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0)) |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
|                                                              |                                                              |

## Extension Methods

| [GetAccessControl(FileStream)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemaclextensions.getaccesscontrol?view=net-5.0#System_IO_FileSystemAclExtensions_GetAccessControl_System_IO_FileStream_) | Returns the security information of a file.                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [SetAccessControl(FileStream, FileSecurity)](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemaclextensions.setaccesscontrol?view=net-5.0#System_IO_FileSystemAclExtensions_SetAccessControl_System_IO_FileStream_System_Security_AccessControl_FileSecurity_) | Changes the security attributes of an existing file.         |
| [ConfigureAwait(IAsyncDisposable, Boolean)](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskasyncenumerableextensions.configureawait?view=net-5.0#System_Threading_Tasks_TaskAsyncEnumerableExtensions_ConfigureAwait_System_IAsyncDisposable_System_Boolean_) | Configures how awaits on the tasks returned from an async disposable are performed. |

## Applies to

| Product         | Versions                                                     |
| :-------------- | :----------------------------------------------------------- |
| .NET            | 5.0, 6.0 Preview 7                                           |
| .NET Core       | 1.0, 1.1, 2.0, 2.1, 2.2, 3.0, 3.1                            |
| .NET Framework  | 1.1, 2.0, 3.0, 3.5, 4.0, 4.5, 4.5.1, 4.5.2, 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1, 4.7.2, 4.8 |
| .NET Standard   | 1.3, 1.4, 1.6, 2.0, 2.1                                      |
| UWP             | 10.0                                                         |
| Xamarin.Android | 7.1                                                          |
| Xamarin.iOS     | 10.8                                                         |
| Xamarin.Mac     | 3.0                                                          |

## See also

- [File](https://docs.microsoft.com/en-us/dotnet/api/system.io.file?view=net-5.0)
- [FileAccess](https://docs.microsoft.com/en-us/dotnet/api/system.io.fileaccess?view=net-5.0)
- [FileMode](https://docs.microsoft.com/en-us/dotnet/api/system.io.filemode?view=net-5.0)
- [FileShare](https://docs.microsoft.com/en-us/dotnet/api/system.io.fileshare?view=net-5.0)
- [File and Stream I/O](https://docs.microsoft.com/en-us/dotnet/standard/io/)
- [How to: Read Text from a File](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-read-text-from-a-file)
- [How to: Write Text to a File](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-write-text-to-a-file)
- [How to: Read and Write to a Newly Created Data File](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-read-and-write-to-a-newly-created-data-file)
