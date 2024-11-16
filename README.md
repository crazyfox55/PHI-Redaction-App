# Build and Run

Only runs on Windows.

Using Visual Studio this project should just run.

Otherwise if you have the .Net CLI tool installed. Run these commands from within the project directory aka '\PHI Redaction App\PHI Redaction App'. Run `dotnet build` followed by `dotnet run` you'll see a WPF desktop application.

# Approach and assumptions

I used a view model to capture the properties and make binding easier than within the main window.
I used command objects, this made the process button automatically disabled when the process command shouldn't execute.

I used Regex lookbehind to replace the PHI based on the labels in the example.

## Optimizations
The reading of the input files executes line by line. This will use the minimal amount of memory and will allow large files to be processed.
The task scheduler handles all of the files in parallel. In this way multiple files can be processing at the same time on different threads.
The processing of files is asynchronous such that any IO operations will not hog the thread.

## Snapshot Testing
Snap shot testing provides a unique way to make sure that the processed file looks correct without having to code it directly into the logic. Instead there is a verified file with the redacted PHI.

## Considerations
I didn't use an Angular frontend. I think it's better to process the PHI directly on the client where the files exist rather than sending them over the network to a C# backend. Therefore this regex implementation could have been done within Typescript client side, however because most of this assignment was directed towards a C# implementation and a generic GUI, I went with WPF for the frontend.

If PHI is going to be sent over the network then HTTPS is a must and secure servers which are able to process PHI will need to be used.