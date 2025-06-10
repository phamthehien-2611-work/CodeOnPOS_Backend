using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using UserManagementService.Protos; // Namespace sinh ra từ file user.proto

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Ocelot từ file ocelot.json
//builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
//builder.Services.AddOcelot(builder.Configuration);

// 2. Đăng ký gRPC client để ApiGateway có thể gọi đến UserManagementService
//    Sử dụng AddGrpcClient để tích hợp với DI và quản lý vòng đời của channel.
builder.Services
    .AddGrpcClient<UserAuthService.UserAuthServiceClient>(o =>
    {
        // Lấy địa chỉ của UserManagementService từ appsettings.json để dễ dàng thay đổi
        var serviceAddress = builder.Configuration["GrpcServices:UserManagement"];
        if (string.IsNullOrEmpty(serviceAddress))
        {
            throw new InvalidOperationException("gRPC service address 'GrpcServices:UserManagement' is not configured.");
        }
        o.Address = new Uri(serviceAddress);
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

// await app.UseOcelot();

app.Run();
