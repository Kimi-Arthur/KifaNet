dotnet publish -c Release src\Pimix.Apps.FileUtil
dotnet publish -c Release src\Pimix.Apps.JobUtil

rmdir /s /q C:\Software\lib\fileutil
rmdir /s /q C:\Software\lib\jobutil

md C:\Software\bin
md C:\Software\lib\fileutil
md C:\Software\lib\jobutil

xcopy /s /y src\Pimix.Apps.FileUtil\bin\Release\netcoreapp2.0\publish\* C:\Software\lib\fileutil
xcopy /s /y src\Pimix.Apps.JobUtil\bin\Release\netcoreapp2.0\publish\* C:\Software\lib\jobutil

copy tools\fileutil.bat C:\Software\bin
copy tools\jobutil.bat C:\Software\bin