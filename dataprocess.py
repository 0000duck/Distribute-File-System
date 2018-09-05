#统计通话次数总和，对于每一块数据
import os
import sys
import time
import re

filepath=sys.argv[1]
filename=sys.argv[2]
num=sys.argv[3]


def CallCount():
    a=[]
    c=[]
    print("开始统计...")
    with open(filepath+filename,'r') as input_file: 
        for line in input_file:
            split_info = line.strip().split('\t')
            a.append(split_info[1])

    b=sorted(a)#排序

    temp=b[0]
    count=0

    for j in b:
        if(j==temp):
             count+=1
             if(j==b[len(b)-1]):
                 c.append(temp+'\t'+str(count))
        else:
            if(j==b[len(b)-1]):
                 c.append(temp+'\t'+str(1))
            else:
                c.append(temp+'\t'+str(count))
                temp=j
                count=1
        
    with open(filepath+'result1_'+num+'.txt', 'w') as fl:
        for i in c:
            fl.write(i)
            fl.write('\n')          
    print("统计结束！")

def  itercolumn412(filename, splitregex = '\t'):
    with open(filename, 'rt') as handle:
        for ln in handle:
            items = re.split(splitregex, ln)
            yield items[4], items[12]

def  OptrType():
    print("开始统计...")
    #1代表移动，2代表联通，3代表电信
    i1=i2=i3=j1=j2=j3=k1=k2=k3=0
    #x代表运营商，y代表通话类型
    for x, y in itercolumn412(filepath+filename, splitregex='\s+'):
            if x == '1'and y == '1':
                    i1+=1
            if x == '2' and y == '1':
                    i2+=1
            if x == '3' and y == '1':
                    i3+=1
            if x == '1'and y == '2':
                    j1+= 1
            if x == '2' and y == '2':
                    j2+=1
            if x == '3' and y == '2':
                    j3+=1
            if x == '1'and y == '3':
                    k1+=1
            if x == '2' and y == '3':
                    k2+=1
            if x == '3' and y == '3':
                    k3+=1
    #i代表市话，j代表长途，k代表漫游
    b=[i1,i2,i3,j1,j2,j3,k1,k2,k3]
    fl=open(filepath+'result2_'+num+'.txt', 'w')
    for i in b:
            fl.write(str(i)+"\t")
    print("统计完成！")
    
CallCount()
OptrType()

print ("计算完成！")
