import { Directive, ElementRef, HostListener, Input, inject } from '@angular/core';

export type InputRule = 'name' | 'digits' | 'phone' | 'email' | 'place' | 'address' | 'plate' | 'decimal';

@Directive({
  selector: '[appInputRule]',
  standalone: true
})
export class InputRuleDirective {

  @Input('appInputRule') rule: InputRule = 'name';

  @Input() ruleMaxLength: number | null = null;

  private readonly element = inject(ElementRef<HTMLInputElement | HTMLTextAreaElement>);

  private isApplying = false;

  @HostListener('input')
  onInput(): void {
    if (this.isApplying) {
      return;
    }

    const input = this.element.nativeElement;
    const sanitized = this.sanitize(input.value);

    if (sanitized === input.value) {
      return;
    }

    this.isApplying = true;
    input.value = sanitized;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    this.isApplying = false;
  }

  @HostListener('blur')
  onBlur(): void {
    const input = this.element.nativeElement;
    const trimmed = input.value.replace(/\s{2,}/g, ' ').trim();

    if (trimmed === input.value) {
      return;
    }

    this.isApplying = true;
    input.value = trimmed;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    this.isApplying = false;
  }

  private sanitize(value: string): string {
    const limit = this.ruleMaxLength;
    let result = value;

    switch (this.rule) {
      case 'name':
        result = value.replace(/[^A-Za-zÇĞİÖŞÜçğıöşü ]/g, '').replace(/^ +/, '').replace(/ {2,}/g, ' ');
        break;
      case 'place':
        result = value.replace(/[^A-Za-zÇĞİÖŞÜçğıöşü .'-]/g, '').replace(/^ +/, '').replace(/ {2,}/g, ' ');
        break;
      case 'digits':
        result = value.replace(/\D/g, '');
        break;
      case 'phone':
        result = value.replace(/\D/g, '').replace(/^90/, '').replace(/^0/, '').slice(0, 10);
        break;
      case 'email':
        result = value.replace(/\s/g, '').toLowerCase();
        break;
      case 'address':
        result = value.replace(/^ +/, '').replace(/ {2,}/g, ' ');
        break;
      case 'plate':
        result = value.toLocaleUpperCase('tr-TR').replace(/[^0-9A-Z ]/g, '');
        break;
      case 'decimal':
        result = value.replace(/[^0-9.,]/g, '').replace(',', '.');
        break;
    }

    return limit ? result.slice(0, limit) : result;
  }
}
