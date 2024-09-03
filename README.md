# IL2CPP Patcher
 ![](https://github.com/user-attachments/assets/f6fe3690-1937-4903-8cf6-3743cead47f2)
 
## Usage ##
1. Open or drag and drop the libil2cpp.so or GameAssembly.dll file into the program window.
2. Enter the information from the functions to modify (name, offset, and the value you want it to return).
3. Click on Apply patches.

## Supported return values ##
• Integer:                    1, 0x01

• Float:                        1f, 1.0, 1.0f

• Double:                    1d, 1.0d

• Boolean:                   True, False

• Byte array:                { 01 00 A0 E3 1E FF 2F E1 }

• Nothing:                   null, return

• ASM:                        mov r0, 0x01; bx lr

## Other features ##
• Allow switching between ARM & INTEL assembler
• Define the base address of the file, which will be used by the assembler
• Define and use constants

## Defining constants ##
 ![](https://github.com/user-attachments/assets/27ca98f6-b6da-4c5d-af06-55cadd26411d)
