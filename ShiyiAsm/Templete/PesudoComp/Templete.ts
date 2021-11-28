import { PesudoCompnent } from "@Root/ShiyiFramework/ShiyiPesudoCompnent/PesudoCompnent";
import { {{ShiyiAsm:Templete}}CompData } from "./Model/Model";
import { {{ShiyiAsm:Templete}}UI } from "./UI/{{ShiyiAsm:Templete}}UI";
import { {{ShiyiAsm:Templete}}Func } from "./Functional/{{ShiyiAsm:Templete}}Func";

export class {{ShiyiAsm:Templete}} extends PesudoCompnent{
    public Func=new {{ShiyiAsm:Templete}}Func<{{ShiyiAsm:Templete}}>();
    public UI=new {{ShiyiAsm:Templete}}UI<{{ShiyiAsm:Templete}}>();
    public data:{{ShiyiAsm:Templete}}CompData={
        Theme: "",
    }
}