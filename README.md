# ShiyiAsm

用于解决微信小程序typescript无法绝对路径引用的问题。

原理：

​	替换字符串，所以请注意alias的key不要和代码重复，否则会被替换，可以使在前加特殊符号的方式表示

使用方法：

1. 将ShiyiAsm解压至tsconfig.json同级目录下

2. 配置ShiyiAsm.xml

   ```xml
   <FuckDotdot>
     <AliasSettings>
       <GlobalAlias key="@ShiyiFramework">miniprogram/ShiyiFramework</GlobalAlias> <!-- GlobalAlias标签定义全局应用的目录别名 -->
     </AliasSettings>
     <TargetFileTypes>
       <FileType type="*.js"></FileType>
       <FileType type="*.json">	
         <Alias key="@Npm">miniprogram/miniprogram_npm</Alias>
       </FileType>
       <FileType type="*.src.wxss" to="*.wxss">	<!-- to属性定义输出后修改的扩展名 -->
         <Alias key="@Npm">miniprogram/miniprogram_npm</Alias> <!-- Alias标签定义目录别名 -->
       </FileType>
       <FileType type="*.saml" to="*.wxml"></FileType>
     </TargetFileTypes>
     <ExcludeSetting>
       <Exclude>miniprogram/miniprogram_npm</Exclude>	<!-- Exclude标签定义需要跳过的目录 -->
       <Exclude>node_modules</Exclude>
     </ExcludeSetting>
     <Command>
       <BeforeCmd>tsc</BeforeCmd>  <!-- BeforeCmd标签定义在替换程序执行前运行的命令，可多个 -->
       <AfterCmd>echo completed</AfterCmd>  <!-- AfterCmd标签定义在替换程序执行后运行的命令，可多个 -->
     </Command>
   </FuckDotdot>
   ```

   注意：

   ​	如果是**源码ts中的引入，FileType的值应该是js**。

   ​	Alias的路径**没有 " ./ "**，末尾也**不加 " / "** 

   ​	正反斜杠无所谓

   ​	FileType中的to表示会将结果输出到带有新的扩展名的文件中

   ​	Exclude表示需要跳过的目录

   ​	可以在.vscode/setting,json中通过添加如下代码将输出文件隐藏

   ```json
   {
       "files.exclude": {
           "**/*.wxss": {
               "when": "$(basename).src.wxss" //当存在.src.wxss同名文件时将.wxss隐藏
           }
       }
   }
   ```

   对于其他的配置了FileType文件同样有效，不限于js，但对于其他文件会直接替换掉Alias，请注意备份，以免误操作

3. Asm功能

   对于定义了的FileType，ShiyiAsm会尝试将{{Ref:[key]}}替换成相应key.saml文件的内容

   ```xml
   -pages
   	-index
   		-Asm
   			Home.saml
   		-index.saml
   		
   index.saml
   =======================================
   <view>
       {{Ref:Home}}
   </view>
   =======================================
   
   Home.saml
   =======================================
   <view>Here is Content of Home</view>
   =======================================
   
   运行程序后
   -pages
   	-index
   		-Asm
   			Home.saml
   		-index.saml
   		-index.wxml
   		
   index.wxml
   =======================================
   <view>
       <view>Here is Content of Home</view>
   </view>
   =======================================
   
   ```

   

4. tsconfig.json中添加paths

   ```json
   {
     "compilerOptions": {
       "baseUrl": "./",
       "paths": {
         "@ShiyiFramework/*":["./miniprogram/ShiyiFramework/*"]
       }
   }
   ```

5. 代码中引用

   ```typescript
   import { ShiyiPageBase } from "@ShiyiFramework/ShiyiPage/Base/ShiyiPageBase";
   ```

6. 将微信开发者工具的 详情 --> 本地设置 --> 自定义处理命令替换成以下命令

   ```
    npm run tsc && ShiyiAsm.exe //若ShiyiAsm.xml中配置了BeforeCmd为tsc，可不加npm run tsc &&
   ```

7. 开始编译

8. 参数 **--w=[sec]**自动检测更改 ，sec为可选参数，用于设定在检测到变更后sec毫秒内无变更执行替换操作

   ```
   ./ShiyiAsm.exe --w=200
   ```

   

