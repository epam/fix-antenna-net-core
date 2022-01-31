<?xml version="1.0" encoding="UTF-8"?> 
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:param name="dict-id" />
<xsl:template match="/">
<fixdic xmlns="http://www.b2bits.com/FIXProtocol" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.b2bits.com/FIXProtocol fixdic.xsd" id="{$dict-id}">
	<xsl:attribute name="fixversion">
		<xsl:if test="/fix/@type = 'FIXT'">T</xsl:if>
		<xsl:value-of select="/fix/@major"/>.<xsl:value-of select="/fix/@minor"/>
		</xsl:attribute>
	<xsl:attribute name="title"><xsl:value-of select="/fix/@type"/> <xsl:value-of select="/fix/@major"/>.<xsl:value-of select="/fix/@minor"/>
		<xsl:if test="/fix/@servicepack &gt; 0"> SP<xsl:value-of select="/fix/@servicepack"/></xsl:if>
	</xsl:attribute>
	<typelist>
		<typedef type="int" />
		<typedef type="float" />
		<typedef type="char" />
		<typedef type="time" />
		<typedef type="date" />
		<typedef type="data" />
		<typedef type="String" />
		<typedef type="Pattern" />
		<typedef type="Price" extends="float" />
		<typedef type="Amt" extends="float" />
		<typedef type="Qty" extends="float" />
		<typedef type="Currency" extends="String" />
		<typedef type="MultipleValueString" extends="String" valuetype="String" />
		<typedef type="MultipleCharValue" extends="String" valuetype="char" />
		<typedef type="Exchange" extends="String" />
		<typedef type="UTCTimestamp" extends="String" />
		<typedef type="Boolean" extends="char" />
		<typedef type="LocalMktDate" extends="String" />
		<typedef type="PriceOffset" extends="float" />
		<typedef type="month-year" extends="String" />
		<typedef type="day-of-month" extends="String" />
		<typedef type="UTCDate" extends="String" />
		<typedef type="UTCDateOnly" extends="String" />
		<typedef type="UTCTimeOnly" extends="String" />
		<typedef type="NumInGroup" extends="int" />
		<typedef type="Percentage" extends="float" />
		<typedef type="SeqNum" extends="int" />
		<typedef type="Length" extends="int" />
		<typedef type="Country" extends="String" />
		<typedef type="Tenor" extends="Pattern" />
		<typedef type="TZTimeOnly" extends="String" />
		<typedef type="TZTimestamp" extends="String" />
		<typedef type="Reserved100Plus" extends="Pattern" />
		<typedef type="Reserved1000Plus" extends="Pattern" />
		<typedef type="Reserved4000Plus" extends="Pattern" />
		<typedef type="XMLData" extends="String" />
		<typedef type="Language" extends="String" />
	</typelist>
	<fielddic>
		<xsl:for-each select="/fix/fields/field">
			<fielddef tag="{@number}" name="{@name}">
				<xsl:variable name="lenfield">
					<xsl:choose>
						<xsl:when test="@lenfield &gt; 0">
							<xsl:value-of select="@lenfield" />
						</xsl:when>
						<xsl:when test="(@type = 'DATA' or @type = 'XMLDATA') and not(@lenfield &gt; 0) and @number &gt; 1 and /fix/fields/field[@number = current()/@number - 1]/@type = 'LENGTH'">
							<xsl:value-of select='/fix/fields/field[@number = current()/@number - 1]/@number' />
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="-1" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:attribute name="type">
					<xsl:choose>
						<xsl:when test="(@type = 'DATA' or @type = 'XMLDATA') and $lenfield = -1">
							<xsl:value-of select="'String'" />
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="convert-type">
								<xsl:with-param name="source-type" select="@type" />
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<xsl:if test="$lenfield != -1">
					<xsl:attribute name="lenfield">
						<xsl:value-of select="$lenfield" />
					</xsl:attribute>
				</xsl:if>
				<xsl:choose>
					<xsl:when test="@type = 'MULTIPLEVALUESTRING'">
						<multi>
							<xsl:for-each select="value">
								<item id="{@description}" val="{@enum}" />
							</xsl:for-each>
						</multi>
					</xsl:when>
					<xsl:otherwise>
						<xsl:for-each select="value">
							<item id="{@description}" val="{@enum}"><xsl:value-of select="@description" /></item>
						</xsl:for-each>
					</xsl:otherwise>
				</xsl:choose>
			</fielddef>
		</xsl:for-each>
	</fielddic>
	<msgdic>
		<xsl:if test="count(/fix/header/field) &gt; 0 or count(/fix/header/group) &gt; 0">
			<xsl:call-template name="blockdef">
				<xsl:with-param name="id" select="'SMH'" />
				<xsl:with-param name="name" select="'Standard Message Header'" />
				<xsl:with-param name="node" select="/fix/header" />
			</xsl:call-template>
		</xsl:if>
		<xsl:if test="count(/fix/trailer/field) &gt; 0 or count(/fix/trailer/group) &gt; 0">
			<xsl:call-template name="blockdef">
				<xsl:with-param name="id" select="'SMT'" />
				<xsl:with-param name="name" select="'Standard Message Trailer'" />
				<xsl:with-param name="node" select="/fix/trailer" />
			</xsl:call-template>
		</xsl:if>
		<xsl:for-each select="/fix/components/component">
			<xsl:call-template name="blockdef">
				<xsl:with-param name="id" select="current()/@name" />
				<xsl:with-param name="name" select="current()/@name" />
				<xsl:with-param name="node" select="current()" />
			</xsl:call-template>
		</xsl:for-each>
		<xsl:for-each select="/fix/messages/message">
			<xsl:call-template name="msgdef">
				<xsl:with-param name="node" select="current()" />
			</xsl:call-template>
		</xsl:for-each>
		
	</msgdic>
</fixdic>
</xsl:template>

<xsl:template name="blockdef">
	<xsl:param name="id" />
	<xsl:param name="name" />
	<xsl:param name="node" />
	<blockdef id="{$id}" name="{$name}">
		<xsl:call-template name="group-content">
			<xsl:with-param name="node" select="$node" />
		</xsl:call-template>
	</blockdef>
</xsl:template>

<xsl:template name="group">
	<xsl:param name="node" />
	<field tag="{/fix/fields/field[@name=$node/@name]/@number}" name="{/fix/fields/field[@name=$node/@name]/@name}" req="{$node/@required}" />	
	<group nofield="{/fix/fields/field[@name=$node/@name]/@number}">
		<xsl:attribute name="startfield">
			<xsl:call-template name="group-startfield">
				<xsl:with-param name="node" select="$node" />
			</xsl:call-template>
		</xsl:attribute>
		<xsl:call-template name="group-content">
			<xsl:with-param name="node" select="$node" />
		</xsl:call-template>
	</group>
</xsl:template>

<xsl:template name="group-startfield">
	<xsl:param name="node" />
	<xsl:choose>
		<xsl:when test="local-name($node/*[1]) = 'field' or local-name($node/*[1]) = 'group'">
			<xsl:value-of select="/fix/fields/field[@name=$node/*[1]/@name]/@number" />
		</xsl:when>
		<xsl:otherwise>
			<!-- component -->
			<xsl:call-template name="group-startfield">
				<xsl:with-param name="node" select="/fix/components/component[@name = $node/*[1]/@name]" />
			</xsl:call-template>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template name="group-content">
	<xsl:param name="node" />
	<xsl:for-each select="$node/*">
		<xsl:choose>
			<xsl:when test="local-name() = 'field'">
				<field tag="{/fix/fields/field[@name=current()/@name]/@number}" name="{@name}" req="{@required}" />
			</xsl:when>
			<xsl:when test="local-name() = 'component'">
				<block idref="{@name}" req="{/fix/components/component[@name=current()/@name]/*[1]/@required}" />
			</xsl:when>
			<xsl:when test="local-name() = 'group'">
				<xsl:call-template name="group">
					<xsl:with-param name="node" select="current()" />
				</xsl:call-template>
			</xsl:when>
		</xsl:choose>
	</xsl:for-each>
</xsl:template>

<xsl:template name="msgdef">
	<xsl:param name="node" />
	<msgdef msgtype="{$node/@msgtype}" name="{$node/@name}">
		<xsl:if test="$node/@msgcat = 'admin'">
			<xsl:attribute name="admin">Y</xsl:attribute>
		</xsl:if>
		<xsl:call-template name="group-content">
			<xsl:with-param name="node" select="$node" />
		</xsl:call-template>
	</msgdef>
</xsl:template>

<xsl:template name="convert-type">
	<xsl:param name="source-type" />
	<xsl:choose>
		<xsl:when test="$source-type = 'STRING'">String</xsl:when>
		<xsl:when test="$source-type = 'CHAR'">char</xsl:when>
		<xsl:when test="$source-type = 'PRICE'">Price</xsl:when>
		<xsl:when test="$source-type = 'INT'">int</xsl:when>
		<xsl:when test="$source-type = 'AMT'">Amt</xsl:when>
		<xsl:when test="$source-type = 'QTY'">Qty</xsl:when>
		<xsl:when test="$source-type = 'CURRENCY'">Currency</xsl:when>
		<xsl:when test="$source-type = 'MULTIPLEVALUESTRING'">MultipleValueString</xsl:when>
		<xsl:when test="$source-type = 'MULTIPLESTRINGVALUE'">String</xsl:when>
		<xsl:when test="$source-type = 'MULTIPLECHARVALUE'">MultipleCharValue</xsl:when>
		<xsl:when test="$source-type = 'EXCHANGE'">Exchange</xsl:when>
		<xsl:when test="$source-type = 'UTCTIMESTAMP'">UTCTimestamp</xsl:when>
		<xsl:when test="$source-type = 'BOOLEAN'">Boolean</xsl:when>
		<xsl:when test="$source-type = 'LOCALMKTDATE'">LocalMktDate</xsl:when>
		<xsl:when test="$source-type = 'DATA'">data</xsl:when>
		<xsl:when test="$source-type = 'FLOAT'">float</xsl:when>
		<xsl:when test="$source-type = 'PRICEOFFSET'">PriceOffset</xsl:when>
		<xsl:when test="$source-type = 'MONTHYEAR'">month-year</xsl:when>
		<xsl:when test="$source-type = 'DAYOFMONTH'">day-of-month</xsl:when>
		<xsl:when test="$source-type = 'UTCDATE'">UTCDate</xsl:when>
		<xsl:when test="$source-type = 'UTCDATEONLY'">UTCDateOnly</xsl:when>
		<xsl:when test="$source-type = 'UTCTIMEONLY'">UTCTimeOnly</xsl:when>
		<xsl:when test="$source-type = 'NUMINGROUP'">NumInGroup</xsl:when>
		<xsl:when test="$source-type = 'PERCENTAGE'">Percentage</xsl:when>
		<xsl:when test="$source-type = 'SEQNUM'">SeqNum</xsl:when>
		<xsl:when test="$source-type = 'LENGTH'">Length</xsl:when>
		<xsl:when test="$source-type = 'COUNTRY'">Country</xsl:when>
		<xsl:when test="$source-type = 'TIME'">UTCTimestamp</xsl:when>
		<xsl:when test="$source-type = 'DATE'">date</xsl:when>
		<xsl:when test="$source-type = 'TZTIMEONLY'">TZTimeOnly</xsl:when>
		<xsl:when test="$source-type = 'TZTIMESTAMP'">TZTimestamp</xsl:when>
		<xsl:when test="$source-type = 'PATTERN'">Pattern</xsl:when>
		<xsl:when test="$source-type = 'TENOR'">Tenor</xsl:when>
		<xsl:when test="$source-type = 'RESERVED100PLUS'">Reserved100Plus</xsl:when>
		<xsl:when test="$source-type = 'RESERVED1000PLUS'">Reserved1000Plus</xsl:when>
		<xsl:when test="$source-type = 'RESERVED4000PLUS'">Reserved4000Plus</xsl:when>
		<xsl:when test="$source-type = 'XMLDATA'">XMLData</xsl:when>
		<xsl:when test="$source-type = 'LANGUAGE'">Language</xsl:when>
		<xsl:otherwise>String</xsl:otherwise>
	</xsl:choose>
</xsl:template>

</xsl:stylesheet>
