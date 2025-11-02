using System;
using System.Net;
using CoreBanking.Application.Common.Models;
using MediatR;

namespace CoreBanking.Application.Common.Interfaces;

public interface ICommand : IRequest<Result> { }

public interface ICommand<TResponse> : IRequest<Result<TResponse>> {}
public interface IQuery<TResponse> : IRequest<Result<TResponse>> {}
