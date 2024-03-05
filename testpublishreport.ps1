$codecovUploader = $ENV:USERPROFILE + '\.nuget\packages\codecovuploader\0.7.2\tools\codecov.exe'

& dotnet test /p:AltCover=true
& $codecovUploader -t 554bb7e1-09d2-4f3a-9b72-c2818e370f94