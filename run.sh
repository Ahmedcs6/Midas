#!/bin/bash

dotnet run --project Maidas.Api &
dotnet run --project Maidas.WASM &
wait
