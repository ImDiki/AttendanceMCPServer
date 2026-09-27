# Student Attendance MCP Server

A small **C# / .NET 8 integration project** that connects an AI client to the same local SQL Server attendance database used by the Student Attendance application.

The project was created as an experiment in exposing a focused student lookup tool through an MCP-style JSON-RPC interface.

## Current Implementation

The console application reads JSON-RPC messages from standard input and handles:

- initialization
- tool discovery
- tool calls
- a `get_student_info` lookup by student code

The lookup uses a parameterized SQL query against the local `Students` table and returns the matching student's basic information.

## Relationship to Student Attendance App

This repository is a companion experiment to the larger Student Attendance project. It reuses the attendance database concept so an AI client can request student information without writing SQL manually.

## Tech Stack

- C#
- .NET 8
- Microsoft.Data.SqlClient
- SQL Server LocalDB
- System.Text.Json
- JSON-RPC / MCP-style tool interface

## Project Status

This is a **learning/prototype integration**, not a production server and not a claim of complete MCP protocol coverage.

The implementation is intentionally small and currently concentrated in `Program.cs`.

## Possible Improvements

- Separate database access from protocol handling.
- Replace `AddWithValue` with explicitly typed SQL parameters.
- Add validation and structured error handling.
- Add automated tests.
- Verify interoperability against the intended MCP client and protocol version.
