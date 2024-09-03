# IL2CPP Patcher
 ![2GuTE13 - Imgur](https://github.com/user-attachments/assets/a271be30-91ac-4b9c-86a9-133ca149bd60)

 
## Usage ##
1. Open or drag and drop the libil2cpp.so or GameAssembly.dll file into the program window.
2. Enter the information from the functions to modify (name, offset, and the value you want it to return).
3. Click on Apply patches.

## Supported return values ##
• Integer:                    **1**   |   **0x01**

• Float:                        **1f**   |   **1.0**   |   **1.0f**

• Double:                    **1d**   |   **1.0d**

• Boolean:                   **True**   |   **False**

• Byte array:                **{ 01 00 A0 E3 1E FF 2F E1 }**

• Nothing:                   **null**   |   **return**

• Custom constants:   **@LicenseType.Valid**

• ASM:                        **mov r0, 0x01; bx lr**   |   **mov r0, @LicenseType.Valid; bx lr**

## Other features ##
• Saving and loading patches

• Allow switching between ARM & INTEL assembler

• Define the base address of the file, which will be used by the assembler

• Define and use custom constants


## Defining constants ##
 ![3lebpsg - Imgur](https://github.com/user-attachments/assets/5c669aa0-0ba6-4c73-9e78-1890986e30d9)

