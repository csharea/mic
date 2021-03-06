﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CoreUlitity.configer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace CoreSwagger
{
    public static class SwaggerEngine
    {
        private static string version = string.Empty;
        private static string title = string.Empty;
        private static bool engalbe = false;
        private static string docName = string.Empty;

        /// <summary>
        /// 针对Swagger引用的扩展
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSwaggerPack(this IServiceCollection services)
        {
            try
            {
                var swaggerInfo = ConfigManager.Build.SwaggerConfig;
                if (swaggerInfo != null)
                {
                    if (swaggerInfo != null)
                    {
                        var swg = swaggerInfo;
                        if (swg.Enable)
                        {
                            engalbe = swg.Enable;
                            title = swg.Title;
                            version = swg.Version;
                            docName = swg.SwaggerName;
                            services.AddSwaggerGen(options =>
                            {
                                options.SwaggerDoc(docName, new Microsoft.OpenApi.Models.OpenApiInfo
                                {
                                    Description = swg.SwaggerDesc,
                                    Title = title,
                                    Version = version
                                });
                                if (swg.Authorization)
                                {
                                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                                    {
                                        Description = "请求头中需要添加Jwt授权Token：Bearer Token",
                                        //Name = "Authorization",
                                        Name = "token",
                                        In = ParameterLocation.Header,
                                        Type = SecuritySchemeType.ApiKey
                                    });
                                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                                    {
                                        {
                                            new OpenApiSecurityScheme
                                            {
                                                Reference = new OpenApiReference {
                                                    Type = ReferenceType.SecurityScheme,
                                                    Id = "Bearer"
                                                }
                                            },
                                            new string[] { }
                                        }
                                    });
                                }
                                if (string.IsNullOrWhiteSpace(swg.XmlAssmblyName))
                                    throw new Exception("无效的xml文件,请在配置文件中配置所需的xml文件");
                                var xmlList = swg.XmlAssmblyName.Split(',');
                                foreach (var xml in xmlList)
                                {
                                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
                                    options.IncludeXmlComments(xmlPath);
                                }
                            });
                            if (swg.Authorization)
                            {
                                #region [添加JWT认证]
                                // 添加验证服务
                                object p = services.AddAuthentication(x =>
                                {
                                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                                }).AddJwtBearer(o =>
                                {
                                    //设置token
                                    o.Events = new JwtBearerEvents()
                                    {
                                        OnMessageReceived = context =>
                                        {
                                            var token = context.Request.Headers["token"];
                                            if (!string.IsNullOrEmpty(token))
                                            {
                                                context.Token = context.Request.Headers["token"];
                                            }
                                            //context.Token = context.Request.Headers["token"];
                                            //context.Token = context.Request.Cookies["token"];
                                            return Task.CompletedTask;
                                        }
                                    };
                                    //Token Validation Parameters
                                    o.TokenValidationParameters = new TokenValidationParameters
                                    {
                                        // 是否开启签名认证
                                        ValidateIssuerSigningKey = true,
                                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(swg.JwtSecret)), //密钥
                                        // 发行人验证，这里要和token类中Claim类型的发行人保持一致
                                        ValidateIssuer = true,
                                        ValidIssuer = swg.Issuer,//发行人
                                        ValidateAudience = false,
                                        ValidAudience = (string.IsNullOrWhiteSpace(swg.Audience) ? swg.Issuer : swg.Audience),//接收人
                                        ValidateLifetime = true,
                                        ClockSkew = TimeSpan.Zero
                                    };
                                });
                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建swagger文件发生异常:${ex.Message}");
            }
            return services;
        }

        /// <summary>
        ///  针对Swagger引用的扩展
        /// </summary>
        /// <param name="app"></param>
        public static void UseSwaggerPack(this IApplicationBuilder app)
        {
            try
            {
                if (engalbe)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(option =>
                    {
                        option.SwaggerEndpoint($"/swagger/{docName}/swagger.json", docName);
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Use Swagger发生异常，ex:{ex.Message}");
            }
        }
    }
}
