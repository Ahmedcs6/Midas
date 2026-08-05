global using System;
global using System.Linq;
global using System.Text;
global using System.Collections.Generic;
global using System.Threading.Tasks;
global using System.ComponentModel.DataAnnotations;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Midas.Api.Models;
global using Midas.Api.Models.Dtos;
global using Midas.Api.Models.Dtos.Auth.Request;
global using Midas.Api.Models.Dtos.Auth.Response;
global using Midas.Api.Models.Dtos.User.Response;

global using Midas.Api.Configuration;
global using Midas.Api.Data;
global using Midas.Api.Helpers;
global using Midas.Api.Interfaces;
global using Midas.Api.Services;

