using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Read an embedded resource without loading or executing the installer assembly.
if (args.Length != 3) throw new ArgumentException("managed-assembly resource-name output-file");
using var source = File.OpenRead(args[0]); using var pe = new PEReader(source); var reader = pe.GetMetadataReader();
var resource = reader.ManifestResources.Select(h => reader.GetManifestResource(h)).Single(r => reader.GetString(r.Name) == args[1]);
if (!resource.Implementation.IsNil) throw new InvalidDataException("Only embedded resources are supported.");
int rva = pe.PEHeaders.CorHeader?.ResourcesDirectory.RelativeVirtualAddress ?? throw new InvalidDataException("Managed PE required.");
var bytes = pe.GetSectionData(rva).GetContent(); int offset = checked((int)resource.Offset);
int size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));
if (size < 0 || size > 1024 * 1024 * 1024 || (long)offset + 4 + size > bytes.Length) throw new InvalidDataException("Invalid resource size.");
using var output = new FileStream(args[2], FileMode.CreateNew); output.Write(bytes.AsSpan(offset + 4, size)); output.Flush(true);
Console.WriteLine("Embedded resource extracted without running installer code.");
