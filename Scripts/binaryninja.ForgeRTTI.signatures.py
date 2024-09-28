# TagRegisterRTTI(callbackStorage, callback) -- used to get global type handlers
"""
48c70100000000     mov     qword [rcx], 0x0
48895108           mov     qword [rcx+0x8], rdx
488b05????????     mov     rax, qword [rel .data]
488901             mov     qword [rcx], rax
488bc1             mov     rax, rcx
48890d????????     mov     qword [rel .data], rcx
c3                 retn
"""

# TagSetHeader(serializer, name, zero, hash) (overloaded) -- used to get names, usually stripped (not mandatory)
"""
48895c2410         mov     qword [rsp+0x10], rbx
48896c2418         mov     qword [rsp+0x18], rbp
4889742420         mov     qword [rsp+0x20], rsi
57                 push    rdi
4156               push    r14
4157               push    r15
4883ec40           sub     rsp, 0x40
8b81b8000000       mov     eax, dword [rcx+0xb8]
33db               xor     ebx, ebx
488bac2480000000   mov     rbp, qword [rsp+0x80]
418bf1             mov     esi, r9d
4d8bf0             mov     r14, r8
4c8bfa             mov     r15, rdx
488bf9             mov     rdi, rcx
83f801             cmp     eax, 0x1
"""

# TagCreateClass(rtti, buffer) -- constructs a class via an rtti
"""
4056               push    rsi
4883ec20           sub     rsp, 0x20
488bf1             mov     rsi, rcx
4885d2             test    rdx, rdx
75??               jne     
4c8b05????????     mov     r8, qword [rel .data]
"""

# TagAddClass(manager, rtti) -- registers class RTTIs
"""
4889542410         mov     qword [rsp+0x10], rdx
53                 push    rbx
55                 push    rbp
56                 push    rsi
57                 push    rdi
4156               push    r14
4883ec40           sub     rsp, 0x40
c605b4????????     mov     byte [rel .data], 0x1
"""

# TagAddEnum(manager, rtti) -- registers enum RTTIs
"""
4057               push    rdi {__saved_rdi}
4156               push    r14 {__saved_r14}
4883ec58           sub     rsp, 0x58
488bfa             mov     rdi, rdx
4c8bf1             mov     r14, rcx
4584c0             test    r8b, r8b
0f84????????       je
"""

# LoadBuildId(text, length, storage) -- unique build tag
"""
88542410           mov     byte [rsp+0x10], dl
56                 push    rsi
4155               push    r13
4883ec68           sub     rsp, 0x68
4c8bc9             mov     r9, rcx
488bf1             mov     rsi, rcx
4983e1f0           and     r9, 0xfffffffffffffff0
400fb6ce           movzx   ecx, sil
83e10f             and     ecx, 0xf
0f57c0             xorps   xmm0, xmm0
4d8be8             mov     r13, r8
66410f7401         pcmpeqb xmm0, xmmword [r9]
660fd7c0           pmovmskb eax, xmm0
d3e8               shr     eax, cl
d3e0               shl     eax, cl
85c0               test    eax, eax
"""
