# BankSystem

A simple banking system consisting of `Client`, `Server`, and `BranchOffice` components.  
The client communicates with the branch office over UDP using custom serialization and an `Encryptor` for payload encryption.  
The `Server` and `BranchOffice` communicate over TCP.

Users can authenticate and perform basic banking operations such as deposits, withdrawals, transfers, and balance inquiries.

## Features
- UDP client-to-branch communication (non-blocking, polling-based I/O)
- TCP server-to-branch communication (non-blocking, multiplexed I/O)
- Encryption key input on the server and secure key exchange during client/branch startup
- Authentication using username and password
- Supported transactions:
  - Deposit
  - Withdraw
  - Transfer
  - Balance inquiry
- In-memory data seeding for demonstration purposes

## Tech Stack
- C# 7.3
- .NET Framework 4.7.2
- Visual Studio 2026
- Console applications

## How It Works
1. The client starts and prompts for the BranchOffice UDP port (loopback).
2. The client sends a single byte to request an encryption key from the branch office and stores it for session-level encryption.
3. After successful login, the client:
   - Serializes DTOs using `SerializationHelper`
   - Encrypts payloads using `Encryptor`
   - Sends requests over UDP and waits for responses using socket polling
4. The BranchOffice delegates client requests to the server.
5. The Server and BranchOffice maintain a persistent TCP channel:
   - TCP multiplexing is used to handle multiple logical operations concurrently
   - Coordination, control messages, and aggregation flow through this channel
6. The server decrypts the request, processes it, and returns an encrypted response to the BranchOffice.
7. The BranchOffice forwards the response to the appropriate client.
8. The client decrypts the response and presents the result to the user.

## Communication Details
- UDP (Client ↔ BranchOffice):
  - Non-blocking sockets
  - Read readiness checked using `Socket.Poll(...)`
  - Key exchange timeout: 60 seconds
  - Request/response timeout: 2 seconds
  - Buffer size: 8192 bytes
- TCP (Server ↔ BranchOffice):
  - Multiplexed I/O for concurrent logical operations
  - Designed for reliable coordination and control traffic

## Prerequisites
- Visual Studio 2019 or newer with .NET Framework tooling
- .NET Framework 4.7.2 Developer Pack
- Windows (loopback networking)
