# IM800Asm

A simple assembler for the IM800 ISA.

This project is written in C# using .NET 10 and is designed to be small, straightforward, and easy to understand.
It assembles a single assembly source file into a binary image and can optionally generate symbol and listing files for
debugging.

It currently does not implement macros or conditional assembly. I recommend using m4 or a similar tool for this
functionality.

## Building

Requirements:
- .NET 10 SDK

Build the project: `dotnet build`

Publish a release build: `dotnet publish -c Release`

This project is primarily developed on Linux, but should run anywhere .NET 10 is supported.

## Usage

`IM800Asm [options] <input>`

### Options

| Option           | Description                                    |
|------------------|------------------------------------------------|
| `-o <file>`      | Output binary file (defaults to `<input>.bin`) |
| `-s <file>`      | Output symbol file                             |
| `-l <file>`      | Output listing file                            |
| `--test <file>`  | Run assembler test file                        |
| `-h`, `--help`   | Show help                                      |
| `-v`, `--version` | Show version information                       |

### Example

Assemble a program:
`IM800Asm foo.asm`

Write the output binary to a specific file:
`IM800Asm foo.asm -o bar.bin`

Generate symbols for debugging:
`IM800Asm foo.asm -s foo.sym`

Generate listing file:
`IM800Asm foo.asm -l foo.lst`

Generate symbol and listing files:
`IM800Asm foo.asm -s foo.sym -l foo.lst`

## Output Files

### Binary

The primary output is a flat binary image suitable for loading into the IM800Emu emulator. Or any other implementation,
should it exist.

### Symbol File

The symbol file contains label information that can be used by the IM800Emu emulator for debugging.

The format is line-oriented and pipe-delimited:
`name|type|value`

For labels, addresses are written in hexadecimal. For EQU symbols, values are written in decimal.

Example:
```text
Reset|Label|00000000
Main|Label|00000020
Foo|EQU|-1234
```

### Listing File

The listing file combines the source code and emitted bytes for debugging.

The format is line-oriented and fixed-width, showing address, emitted bytes, and source.

e.g.
```text
00000000: 00 E1 08                 LD A, 8
00000003: 8A 1E                    RET
```

If a line emits more than 8 bytes, the emitted bytes section continues on its own in further lines, broken into 8-byte segments.

## Testing

IM800Asm includes a JSON-based automated test runner. A test file is an array of test cases.

A test case is an object that contains:
- `Name`: Printed test name
- `Source`: Array of source text lines
- `ExpectedOutputHex`: Expected machine code as a hexadecimal string

## License

This project is released under the BSD 3-Clause "New" or "Revised" License
