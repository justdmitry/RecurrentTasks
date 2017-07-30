# General configurations, such as location of the code coverage report, location of the OpenCover and location of .NET 
$resultsFile = 'opencover-result.xml'
$openCoverConsole = $ENV:USERPROFILE + '\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe'
$codecovUploader = $ENV:USERPROFILE + '\.nuget\packages\codecov\1.0.1\tools\codecov.exe'
$target = '-target: C:\Program Files\dotnet\dotnet.exe'

# Rebuild with full debug info
& dotnet build /p:DebugType=full

# Configuration and execution of the tests
$targetArgs = '-targetargs: test test/RecurrentTasks.Tests/RecurrentTasks.Tests.csproj -f netcoreapp1.0 /p:DebugType=full'
$filter = '-filter: +[RecurrentTasks*]* -[RecurrentTasks.Tests*]*'
$output = '-output:' + $resultsFile
& $openCoverConsole $target $targetArgs '-register:user' $filter $output '-oldStyle'

# Upload to codecov.io
& $codecovUploader -f $resultsFile -t 554bb7e1-09d2-4f3a-9b72-c2818e370f94