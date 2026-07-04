#!/bin/bash

dotnet run --project Midas.Api &
dotnet run --project Midas.WASM &
wait
