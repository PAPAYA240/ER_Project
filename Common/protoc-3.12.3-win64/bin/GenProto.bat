protoc.exe -I=./ --csharp_out=./ ./Protocol.proto 
IF ERRORLEVEL 1 PAUSE

START ../../../Server/PacketGenerator/bin/PacketGenerator.exe ./Protocol.proto
XCOPY /Y Protocol.cs "../../../Client/Assets/Scripts/Packet"
XCOPY /Y Protocol.cs "../../../Server/Server/Packet"
XCOPY /Y ClientPacketManager.cs "../../../Client/Assets/Scripts/Packet"
XCOPY /Y ServerPacketManager.cs "../../../Server/Server/Packet"
COPY /Y "..\..\..\Server\Server\Data\json\SkillData.json" "..\..\..\Client\Assets\Resources\Data\SkillData.json"
COPY /Y "..\..\..\Server\Server\Data\json\SkillSpecData.json" "..\..\..\Client\Assets\Resources\Data\SkillSpecData.json"
COPY /Y "..\..\..\Server\Server\Data\json\StatData.json"  "..\..\..\Client\Assets\Resources\Data\StatData.json"
COPY /Y "..\..\..\Server\Server\Data\json\HitboxData.json"  "..\..\..\Client\Assets\Resources\Data\HitboxData.json"
COPY /Y "..\..\..\Server\Server\Data\json\MonsterHitboxData.json"  "..\..\..\Client\Assets\Resources\Data\MonsterData\MonsterHitboxData.json"