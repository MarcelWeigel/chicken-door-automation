
# Build

dotnet publish -c Release -r linux-arm --self-contained=false
• --self-contained=true includes everything needed to run (no runtime needed)
• -p:PublishReadyToRun=true compiles into native code (ARM assembler)
• -p:PublishSingleFile=true compiles into a "fat" single file containing all